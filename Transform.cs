using System.Numerics;

public struct Transform {
	public Transform(Vector3 pos, Quaternion rot) {
		Position = pos;
		Rotation = rot;
	}
	public Vector3 Position;
	public Quaternion Rotation;

	public override bool Equals(object obj) {
		var t = obj as Transform?;
		if (t is null)
			return false;
		if (t.Value.Position != Position)
			return false;
		if (t.Value.Rotation != Rotation)
			return false;
		return true;
	}
}