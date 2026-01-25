using System.Numerics;

public struct Transform {
	public Transform(Vector3 pos, Quaternion rot) {
		Position = pos;
		Rotation = rot;
	}
	public Vector3 Position;
	public Quaternion Rotation;
}