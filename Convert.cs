using System.Reflection;

public class Convert {
	[AttributeUsage(AttributeTargets.Class)]
	public class AssociationAttribute(string[] extensions) : Attribute {
		public HashSet<string> Types = [.. extensions];
	}
	public class Source {
		public virtual Sequence Read(string path) {return null;}
	}

	public static void Main(string[] args) {
		List<string> argacc = [.. args];
		Console.WriteLine("-- bseq converter utility --");
		if (argacc.Count == 0)
			Console.WriteLine("cmd args");
			Console.WriteLine(" <input file>");
		while (!File.Exists(argacc.ElementAtOrDefault(0))) {
			if (argacc.Count > 0) {
				Console.WriteLine($"{argacc[0]} is not a valid file");
				argacc.RemoveAt(0);
			}
			Console.Write("input file: ");
			argacc.Insert(0, Console.ReadLine().Trim());
		}
		Write(argacc[0]);
	}
	public static void Write(string input) {
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
			return;
		}
		using var f = new BinaryWriter(File.OpenWrite($"{input.Split('.').First()}.btex"));
		Write(f, inst.Read(input));
	}

	private static void Write(BinaryWriter f, Sequence s) {
		if (s is null)
			return;
		f.Write("bseq".ToArray());
		f.Write(s.Frames);
		f.Write(s.Rate);
		f.Write(false); //scale
		//bone count, names
		//vec3
		//vec4
		//bind
		//anim
	}
}