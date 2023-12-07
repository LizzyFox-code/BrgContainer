namespace BrgContainer.Runtime
{
    using Unity.Jobs;

    internal struct BatchDescriptionDisposeJob : IJob
    {
        private BatchDescriptionDisposeData m_DisposeData;

        public BatchDescriptionDisposeJob(ref BatchDescriptionDisposeData disposeData)
        {
            m_DisposeData = disposeData;
        }
        
        public void Execute()
        {
            m_DisposeData.Dispose();
        }
    }
}