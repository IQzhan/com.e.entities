namespace E.Entities
{
    public enum EntityGameObjectIncluded
    {
        None = 0,
        Position = 1 << 0,
        Rotation = 1 << 1,
        Scale = 1 << 2,
        Layer = 1 << 3,
        Transform = Position | Rotation | Scale,
        Rigid = Position | Rotation,
        TransformWithLayer = Transform | Layer,
        RigidWithLayer = Rigid | Layer,
    }
}