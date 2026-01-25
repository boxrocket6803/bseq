using Datamodel;
using System.Numerics;

[Convert.Association(["dmx"])]
public class ValveDMX : Convert.Source {
	public override Sequence Read(string path) {
		Sequence s = new();
		var dmx = Datamodel.Datamodel.Load(path);
		LoadSkeleton(dmx.Root, s);
		LoadAnimation(dmx.Root, s);
		return s;
	}

	private void LoadSkeleton(Element dmx, Sequence s) {
		void Unfold(string parent, ElementArray bones) {
			foreach (var bone in bones) {
				var t = bone.Get<Element>("transform");
				s.Skeleton.Add(bone.Name, new() {
					Parent = parent,
					Local = new() {Position = t.Get<Vector3>("position"), Rotation = t.Get<Quaternion>("orientation")}
				});
				Unfold(bone.Name, bone.Get<ElementArray>("children"));
			}
		}
		Unfold(null, dmx.Get<Element>("skeleton").Get<ElementArray>("children"));
	}
	private void LoadAnimation(Element dmx, Sequence s) {
		var animation = dmx.Get<Element>("animationList").Get<ElementArray>("animations").First();
		s.Rate = (byte)animation.Get<float>("frameRate");
		Console.WriteLine($"Rate: {s.Rate}");
		var timeframe = animation.Get<Element>("timeFrame");
		var scale = timeframe.Get<float>("scale");
		var duration = (float)timeframe.Get<TimeSpan>("duration").TotalSeconds;
		var offset = (float)timeframe.Get<TimeSpan>("offset").TotalSeconds;
		var start = (float)timeframe.Get<TimeSpan>("start").TotalSeconds;
		foreach (var channel in animation.Get<ElementArray>("channels")) {
			var toElement = channel.Get<Element>("toElement");
			if (toElement is null)
				continue;
			//Console.WriteLine(toElement.ID);
			//1655
		}
	}
}