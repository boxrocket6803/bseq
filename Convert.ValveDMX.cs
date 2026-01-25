using Datamodel;

[Convert.Association(["dmx"])]
public class ValveDMX : Convert.Source {
	public override Sequence Read(string path) {
		Sequence s = new();
		var dmx = Datamodel.Datamodel.Load(path);
		LoadSkeleton(dmx.Root, s);
		LoadAnimation(dmx.Root, s);
		return base.Read(path);
	}

	private void LoadSkeleton(Element dmx, Sequence s) {
		//1314
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
			Console.WriteLine(toElement.ID);
			//1655
		}
	}
}