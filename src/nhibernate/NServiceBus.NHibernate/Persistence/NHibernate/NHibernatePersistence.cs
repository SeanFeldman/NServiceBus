﻿namespace NServiceBus.Persistence.NHibernate
{
    using Config;
    using Gateway.Persistence;
    using Saga;
    using Timeout.Core;
    using Unicast.Subscriptions;

    public class NHibernatePersistence
    {
        public static void UseAsDefault()
        {
            InfrastructureServices.SetDefaultFor<ISagaPersister>(() => Configure.Instance.UseNHibernateSagaPersister());
            InfrastructureServices.SetDefaultFor<IPersistTimeouts>(() => Configure.Instance.UseNHibernateTimeoutPersister());
            InfrastructureServices.SetDefaultFor<IPersistMessages>(() => Configure.Instance.UseNHibernateGatewayPersister());
            InfrastructureServices.SetDefaultFor<ISubscriptionStorage>(() => Configure.Instance.UseNHibernateSubscriptionPersister());
        }
    }
}