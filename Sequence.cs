public partial class Sequence {
	public uint Frames;
	public byte Rate;
	public Dictionary<string, Transform> Bind = [];
	public Dictionary<string, Transform[]> Transforms = [];
}