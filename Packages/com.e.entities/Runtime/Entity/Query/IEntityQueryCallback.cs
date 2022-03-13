using Unity.Jobs.LowLevel.Unsafe;

namespace E.Entities
{
    [JobProducerType(typeof(IEntityQueryCallbackExtends.JobStruct4<>))]
    public interface IEntityQueryCallback4
    {
        public void Execute(ref QueryResult4 result);
    }

    [JobProducerType(typeof(IEntityQueryCallbackExtends.JobStruct8<>))]
    public interface IEntityQueryCallback8
    {
        public void Execute(ref QueryResult8 result);
    }

    public interface IEntityGroupQueryCallback
    {
        public void Execute(EntityGroup group);
    }
}