namespace bseq;

using System.Numerics;
using System.Reflection;
using System.Threading.Tasks.Dataflow;

public class Convert {
	[AttributeUsage(AttributeTargets.Class)]
	public class AssociationAttribute(string[] extensions) : Attribute {
		public HashSet<string> Types = [.. extensions];
	}
	public class Source {
		public virtual Sequence Read(string path) {return null;}
	}

	public static bool Supports(string file) {
		foreach (var type in Assembly.GetAssembly(typeof(Source)).GetTypes()) {
			if (!type.IsAssignableTo(typeof(Source)))
				continue;
			var assoc = type.GetCustomAttribute<AssociationAttribute>();
			if (assoc is null)
				continue;
			if (!assoc.Types.Contains(file.Split('.').Last()))
				continue;
			return true;
		}
		return false;
	}
	public static void ListSupported() {
		Console.WriteLine("supported types for bseq convert");
		foreach (var type in Assembly.GetAssembly(typeof(Source)).GetTypes()) {
			if (!type.IsAssignableTo(typeof(Source)))
				continue;
			var assoc = type.GetCustomAttribute<AssociationAttribute>();
			if (assoc is null)
				continue;
			foreach (var ext in assoc.Types)
				Console.WriteLine($" .{ext}");
		}
	}
	public static void Main(string[] args) => Run(args);
	public static string Run(string[] args) {
		List<string> argacc = [.. args];
		Console.WriteLine("-- bseq converter utility --");
		if (argacc.Count == 0) {
			Console.WriteLine("cmd args");
			Console.WriteLine(" <input file>");
		}
		while (!File.Exists(argacc.ElementAtOrDefault(0))) {
			if (argacc.Count > 0) {
				Console.WriteLine($"{argacc[0]} is not a valid file");
				argacc.RemoveAt(0);
			}
			Console.Write("input file: ");
			argacc.Insert(0, Console.ReadLine().Trim());
		}
		return Write(argacc[0]);
	}
	public static string Write(string input) {
		Source inst = null;
		foreach (var type in Assembly.GetAssembly(typeof(Source)).GetTypes()) {
			if (!type.IsAssignableTo(typeof(Source)))
				continue;
			var assoc = type.GetCustomAttribute<AssociationAttribute>();
			if (assoc is null)
				continue;
			if (!assoc.Types.Contains(input.Split('.').Last()))
				continue;
			Console.WriteLine($"using {type} converter");
			inst = (Source)type.GetConstructor([]).Invoke([]);
			break;
		}
		if (inst is null) {
			Console.WriteLine($"could not find converter source for '{input.Split('.').Last()}'");
			return null;
		}
		var path = $"{input.Split('.').First()}.bseq";
		if (File.Exists(path))
			File.Delete(path);
		using var f = new BinaryWriter(File.OpenWrite(path));
		Write(f, inst.Read(input));
		return path;
	}

	private static void Write(BinaryWriter f, Sequence s) {
		HashSet<Vector3> vec3h = [];
		HashSet<Quaternion> quath = [];
		foreach (var bone in s.Skeleton.Values) {
			vec3h.Add(bone.Local.Position);
			quath.Add(bone.Local.Rotation);
		}
		if (s.RootMotion is not null) {
			foreach (var key in s.RootMotion) {
				vec3h.Add(key.Position);
				quath.Add(key.Rotation);
			}
		}
		foreach (var track in s.Tracks.Values) {
			foreach (var key in track) {
				vec3h.Add(key.Position);
				quath.Add(key.Rotation);
			}
		}
		Dictionary<Vector3,int> vec3 = [];
		foreach (var val in vec3h)
			vec3.Add(val, vec3.Count);
		Dictionary<Quaternion,int> quat = [];
		foreach (var val in quath)
			quat.Add(val, quat.Count);

		f.Write("bseq".ToArray());
		byte flags = 0;
		if (vec3.Count > byte.MaxValue || quat.Count > byte.MaxValue)
			flags |= (byte)Sequence.Flags.Index16;
		if (vec3.Count > ushort.MaxValue || quat.Count > ushort.MaxValue)
			flags |= (byte)Sequence.Flags.Index32;
		if (s.Tracks.Count != 0)
			flags |= (byte)Sequence.Flags.Animation;
		if (s.Events.Count != 0)
			flags |= (byte)Sequence.Flags.Events;
		if (s.RootMotion is not null && s.RootMotion.Any((rt) => rt != Transform.Zero))
			flags |= (byte)Sequence.Flags.RootMotion;
		f.Write(flags);
		f.Write(s.Rate);
		f.Write(s.Frames);
		//events
		if ((flags & (byte)Sequence.Flags.Events) != 0) {
			f.Write((ushort)s.Events.Count);
			foreach (var frame in s.Events) {
				f.Write((uint)frame.Key);
				f.Write((byte)frame.Value.Count);
				foreach (var evt in frame.Value)
					f.Write(evt);
			}
		}
		//values
		void WriteIndex(int i) {
			if ((flags & (byte)Sequence.Flags.Index32) != 0)
				f.Write((uint)i);
			else if ((flags & (byte)Sequence.Flags.Index16) != 0)
				f.Write((ushort)i);
			else
				f.Write((byte)i);
		}
		WriteIndex(vec3.Count);
		foreach (var val in vec3.Keys) {
			f.Write(val.X);
			f.Write(val.Y);
			f.Write(val.Z);
		}
		WriteIndex(quat.Count);
		foreach (var val in quat.Keys) {
			f.Write(val.X);
			f.Write(val.Y);
			f.Write(val.Z);
			f.Write(val.W);
		}
		//bones
		f.Write((ushort)s.Skeleton.Count);
		Dictionary<string,ushort> indicies = [];
		foreach (var bone in s.Skeleton) {
			indicies.Add(bone.Key, (ushort)indicies.Count);
			f.Write(bone.Key);
			if (bone.Value.Parent is not null)
				f.Write(indicies[bone.Value.Parent]);
			else
				f.Write(ushort.MaxValue);
			WriteIndex(vec3[bone.Value.Local.Position]);
			WriteIndex(quat[bone.Value.Local.Rotation]);
			if ((flags & (byte)Sequence.Flags.Animation) != 0)
				f.Write(s.Tracks.ContainsKey(bone.Key));
		}
		//root motion
		if ((flags & (byte)Sequence.Flags.RootMotion) != 0) {
			foreach (var key in s.RootMotion) {
				WriteIndex(vec3[key.Position]);
				WriteIndex(quat[key.Rotation]);
			}
		}
		//anim
		if ((flags & (byte)Sequence.Flags.Animation) != 0) {
			foreach (var bone in s.Skeleton.Keys) {
				if (!s.Tracks.TryGetValue(bone, out var track))
					continue;
				f.Write(track.Length == 2);
				foreach (var key in track) {
					WriteIndex(vec3[key.Position]);
					WriteIndex(quat[key.Rotation]);
				}
			}
		}
	}
}