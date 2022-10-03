using Unity.Jobs.LowLevel.Unsafe;

namespace E.Entities
{
    [JobProducerType(typeof(IEntityQueryCallbackExtends.JobStruct<>))]
    public interface IEntityQueryCallback
    {
        public void Execute(ref QueryResult result);
    }

    public interface IEntityGroupQueryCallback
    {
        public void Execute(EntityGroup group);
    }
}