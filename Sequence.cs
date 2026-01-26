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
	public struct Event {
		public string Name;
		public uint Data;
	}

	public uint Frames;
	public byte Rate;
	public Dictionary<string, Bone> Skeleton = [];
	public Dictionary<string, Transform[]> Tracks = [];
	public Dictionary<int, Event> Events = [];

	public static Sequence Load(BinaryReader f) {
		Sequence s = new();
		f.ReadBytes(4); //bseq
		var flags = f.ReadByte();
		s.Rate = f.ReadByte();
		s.Frames = f.ReadUInt32();
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
		return s;
	}
}