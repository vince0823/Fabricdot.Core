﻿using System;
using Fabricdot.Domain.Auditing;

namespace Fabricdot.Domain.Entities
{
    public abstract class CreationAuditAggregateRoot<TKey> : AggregateRoot<TKey>, ICreationAuditEntity where TKey : notnull
    {
        /// <inheritdoc />
        public DateTime CreationTime { get; protected set; }

        /// <inheritdoc />
        public string? CreatorId { get; protected set; }
    }
}