using System.Numerics;

public struct Transform {
	public Transform(Vector3 pos, Quaternion rot) {
		Position = pos;
		Rotation = rot;
	}
	public Vector3 Position;
	public Quaternion Rotation;

	public static Transform Zero => new(Vector3.Zero, Quaternion.Identity);
	public static Transform Lerp(Transform a, Transform b, float t, bool clamp) {
		return new Transform {
			Position = Vector3.Lerp( a.Position, b.Position, t ),
			Rotation = Quaternion.Slerp( a.Rotation, b.Rotation, t ),
		};
	}

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