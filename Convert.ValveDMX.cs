using Datamodel;
using System.Numerics;

[Convert.Association(["dmx"])]
public class ValveDMX : Convert.Source {
	public override Sequence Read(string path) {
		Sequence s = new();
		var dmx = Datamodel.Datamodel.Load(path);
		Load(dmx.Root, s);
		return s;
	}

	public static float Remap(float value, float oldLow, float oldHigh) {
		if ( MathF.Abs( oldHigh - oldLow ) < 0.0001f )
			return 0;
		return (value - oldLow) / (oldHigh - oldLow);
	}

	private static void Load(Element dmx, Sequence s) {
		//skeleton
		Dictionary<Guid,string> bonemap = [];
		if (dmx.ContainsKey("skeleton")) {
			var rt = dmx.Get<Element>("skeleton").Get<Element>("transform");
			bonemap.Add(rt.ID, "root_transform");
			s.Skeleton.Add("root_transform", new() {
				Parent = null,
				Local = new() {Position = rt.Get<Vector3>("position"), Rotation = rt.Get<Quaternion>("orientation")}
			});
			void Unfold(string parent, ElementArray bones) {
				foreach (var bone in bones) {
					var t = bone.Get<Element>("transform");
					bonemap.Add(t.ID, bone.Name);
					s.Skeleton.Add(bone.Name, new() {
						Parent = parent,
						Local = new() {Position = t.Get<Vector3>("position"), Rotation = t.Get<Quaternion>("orientation")}
					});
					Unfold(bone.Name, bone.Get<ElementArray>("children"));
				}
			}
			Unfold("root_transform", dmx.Get<Element>("skeleton").Get<ElementArray>("children"));
		} else if (dmx.ContainsKey("camera")) {
			var rt = dmx.Get<Element>("camera").Get<Element>("transform");
			bonemap.Add(rt.ID, "root_transform");
			s.Skeleton.Add("root_transform", new() {
				Parent = null,
				Local = new() {Position = rt.Get<Vector3>("position"), Rotation = rt.Get<Quaternion>("orientation")}
			});
		}
		//animation
		var animation = dmx.Get<Element>("animationList").Get<ElementArray>("animations").First();
		s.Rate = (byte)animation.Get<float>("frameRate");
		Console.WriteLine($"Rate: {s.Rate}");
		var start = (float)animation.Get<Element>("timeFrame").Get<TimeSpan>("start").TotalSeconds;
		var end = 0f;
		foreach (var channel in animation.Get<ElementArray>("channels"))
			end = MathF.Max(end, (float)channel.Get<Element>("log").Get<ElementArray>("layers").First().Get<TimeSpanArray>("times").Last().TotalSeconds);
		if (dmx.ContainsKey("exportTags")) {
			var range = dmx.Get<Element>("exportTags").Get<string>("timeRange").Split('-').ToList();
			if (string.IsNullOrEmpty(range[0])) { //fuck you valve
				range.RemoveAt(0);
				range[0] = $"-{range[0]}";
			}
			if (string.IsNullOrEmpty(range[1])) {
				range.RemoveAt(1);
				range[1] = $"-{range[1]}";
			}
			start = MathF.Max(start, float.Parse(range[0]));
			end = MathF.Min(end, float.Parse(range[1]));
		}
		end += start;
		Console.WriteLine($"Duration: {end}");
		s.Frames = (uint)Math.Round(end * s.Rate);
		Console.WriteLine($"Frames: {s.Frames}");
		HashSet<string> longbones = [];
		foreach (var channel in animation.Get<ElementArray>("channels")) {
			if (channel.Name == "ShotBoundsChannel")
				continue;
			var toElement = channel.Get<Element>("toElement");
			if (toElement is null)
				continue;
			if (!bonemap.TryGetValue(toElement.ID, out var bone)) //TODO flexes
				continue;
			var log = channel.Get<Element>("log").Get<ElementArray>("layers").First();
			var times = log.Get<TimeSpanArray>("times");
			var values = log.Get<object>("values");
			if (!s.Tracks.TryGetValue(bone, out var track))
				track = new Transform[s.Frames];
			var key = 1;
			for (var i = 0; i < track.Length; i++) {
				var transform = track[i];
				if (times.Count > 1) {
					var time = i / (float)s.Rate;
					time -= start;
					while (times[key].TotalSeconds <= time && key + 1 < times.Count)
						key++;
					var delta = Math.Clamp(Remap(time, (float)times[key - 1].TotalSeconds, (float)times[key].TotalSeconds), 0f, 1f);
					switch (log.ClassName) {
						case "DmeVector3LogLayer":
							var vec3 = values as Vector3Array;
							if (vec3.Count > 2)
								longbones.Add(bone);
							transform.Position = Vector3.Lerp(vec3[key-1], vec3[key], delta);
							break;
						case "DmeQuaternionLogLayer":
							var quat = values as QuaternionArray;
							if (quat.Count > 2)
								longbones.Add(bone);
							transform.Rotation = Quaternion.Slerp(quat[key-1], quat[key], delta);
							break;
						default:
							Console.WriteLine(log.ClassName);
							break;
					}
				} else {
					switch (log.ClassName) {
						case "DmeVector3LogLayer":
							transform.Position = (values as Vector3Array)[0];
							break;
						case "DmeQuaternionLogLayer":
							transform.Rotation = (values as QuaternionArray)[0];
							break;
						default:
							Console.WriteLine(log.ClassName);
							break;
					}
				}
				track[i] = transform;
			}
			s.Tracks[bone] = track;
		}
		foreach (var track in s.Tracks) {
			if (!longbones.Contains(track.Key))
				s.Tracks[track.Key] = [track.Value.First(), track.Value.Last()];
		}
	}
}