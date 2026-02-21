using System.Numerics;

public partial class Sequence {
	public enum Flags {
		Index16		= 1 << 0,
		Index36		= 1 << 1,
		Scale		= 1 << 2,
		Animation	= 1 << 3,
		RootMotion	= 1 << 4,
		Events		= 1 << 5,
	}
	public struct Bone {
		public Transform Local;
		public string Parent;
	}
	public struct Pair(int lo, int hi, float delta) {
		public int Lo = lo;
		public int Hi = hi;
		public float Delta = delta;
	}

	public uint Frames;
	public byte Rate;
	public Dictionary<string, Bone> Skeleton = [];
	public Dictionary<string, Transform[]> Tracks = [];
	public Dictionary<int, HashSet<string>> Events = [];
	public float Length => Frames / (float)Rate;

	public Transform? Bind(string bone) {
		if (Skeleton.TryGetValue(bone, out var b))
			return b.Local;
		return null;
	}
	public Pair GetPair(float cycle, bool loop) {
		var playhead = cycle * (loop ? Frames : Frames - 1);
		var lo = (int)MathF.Floor(playhead);
		var hi = (int)MathF.Ceiling(playhead);
		if (hi >= Frames)
			hi = loop ? 0 : (int)Frames - 1;
		var delta = playhead - lo;
		return new(lo, hi, delta);
	}
	public Transform? Sample(string bone, int frame) {
		if (!Tracks.TryGetValue(bone, out var track))
			return Bind(bone);
		return Sample(track, frame);
	}
	public Transform? Sample(string bone, Pair p) {
		if (!Tracks.TryGetValue(bone, out var track))
			return Bind(bone);
		return Transform.Lerp(Sample(track, p.Lo), Sample(track, p.Hi), p.Delta, false);
	}
	public Transform Sample(Transform[] track, int frame) {
		if (track.Length > 2)
			return track[frame];
		if (track.Length == 1)
			return track[0];
		if (track.Length == 0)
			return Transform.Zero;
		return Transform.Lerp(track[0], track[1], frame / (float)Frames, false);
	}

	public static Sequence Load(Stream f) {
		if (f is null)
			return null;
		return Load(new BinaryReader(f));
	}
	public static Sequence Load(BinaryReader f) {
		Sequence s = new();
		f.ReadBytes(4); //bseq
		var flags = f.ReadByte();
		s.Rate = f.ReadByte();
		s.Frames = f.ReadUInt32();
		//events
		if ((flags & (byte)Flags.Events) != 0) {
			var evtcount = f.ReadUInt16();
			for (int i = 0; i < evtcount; i++) {
				var frame = f.ReadUInt32();
				var framecount = f.ReadByte();
				HashSet<string> evts = new(framecount);
				for (int j = 0; j < framecount; j++)
					evts.Add(f.ReadString());
				s.Events[(int)frame] = evts;
			}
		}
		//values
		uint ReadIndex() {
			if ((flags & (byte)Flags.Index36) != 0) return f.ReadUInt32();
			if ((flags & (byte)Flags.Index16) != 0) return f.ReadUInt16();
			return f.ReadByte();
		}
		var vec3 = new Vector3[ReadIndex()];
		for (int i = 0; i < vec3.Length; i++)
			vec3[i] = new(f.ReadSingle(), f.ReadSingle(), f.ReadSingle());
		var quat = new Quaternion[ReadIndex()];
		for (int i = 0; i < quat.Length; i++)
			quat[i] = new(f.ReadSingle(), f.ReadSingle(), f.ReadSingle(), f.ReadSingle());
		//skeleton
		var bones = new string[f.ReadUInt16()];
		var abones = new bool[bones.Length];
		for (int i = 0; i < bones.Length; i++) {
			Bone b = new();
			bones[i] = f.ReadString();
			var parent = f.ReadUInt16();
			if (parent != ushort.MaxValue)
				b.Parent = bones[parent];
			b.Local = new(vec3[ReadIndex()], quat[ReadIndex()]);
			if ((flags & (byte)Flags.Animation) != 0)
				abones[i] = f.ReadBoolean();
			else
				abones[i] = false;
			s.Skeleton.Add(bones[i], b);
		}
		//anim
		if ((flags & (byte)Flags.Animation) != 0) {
			for (int i = 0; i < bones.Length; i++) {
				if (!abones[i])
					continue;
				var track = new Transform[f.ReadBoolean() ? 2 : s.Frames];
				for (int i2 = 0; i2 < track.Length; i2++)
					track[i2] = new(vec3[ReadIndex()], quat[ReadIndex()]);
				s.Tracks.Add(bones[i], track);
			}
		}
		return s;
	}
}
