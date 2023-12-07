namespace BrgContainer.Runtime
{
    using Unity.Jobs;

    internal struct BatchGroupDisposeJob : IJob
    {
        private BatchGroupDisposeData m_DisposeData;

        public BatchGroupDisposeJob(ref BatchGroupDisposeData disposeData)
        {
            m_DisposeData = disposeData;
        }
        
        public void Execute()
        {
            m_DisposeData.Dispose();
        }
    }
}