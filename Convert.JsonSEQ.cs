using System.Numerics;
using System.Text.Json.Nodes;

[Convert.Association(["seq"])]
public class JsonSEQ : Convert.Source {
	public override Sequence Read(string path) {
		Sequence s = new();
		var seq = JsonNode.Parse(File.OpenRead(path));
		Load(seq.AsObject(), s);
		return s;
	}

	private static void Load(JsonObject seq, Sequence s) {
		Transform GetTransform(string key) {
			Vector3 Pos(string str) {
				var split = str.Split(',');
				return new(float.Parse(split[0]), float.Parse(split[1]), float.Parse(split[2]));
			}
			Quaternion Rot(string str) {
				var split = str.Split(',');
				return new(float.Parse(split[0]), float.Parse(split[1]), float.Parse(split[2]), float.Parse(split[3]));
			}
			var split = key.Split(',');
			var poskey = int.Parse(split[0]) - 1;
			var pos = Vector3.Zero;
			if (poskey >= 0)
				pos = Pos((string)seq["Positions"][poskey].AsValue());
			var rotkey = int.Parse(split[1]) - 1;
			var rot = Quaternion.Identity;
			if (rotkey >= 0)
				rot = Rot((string)seq["Rotations"][rotkey].AsValue());
			return new(pos, rot);
		}
		s.Rate = (byte)seq["Framerate"];
		Console.WriteLine($"Rate: {s.Rate}");
		s.Frames = (uint)seq["Length"];
		Console.WriteLine($"Frames: {s.Frames}");
		foreach (var bone in seq["Bones"].AsObject()) {
			s.Skeleton.Add(bone.Key, new() {
				Local = GetTransform((string)seq["Frames"][0][(int)bone.Value.AsValue()].AsValue()),
				Parent = null,
			});
			var track = new Transform[s.Frames];
			for (int i = 0; i < track.Length; i++)
				track[i] = GetTransform((string)seq["Frames"][i][(int)bone.Value.AsValue()].AsValue());
			s.Tracks.Add(bone.Key, track);
		}
	}
}