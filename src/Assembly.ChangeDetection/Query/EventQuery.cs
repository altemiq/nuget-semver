// -----------------------------------------------------------------------
// <copyright file="EventQuery.cs" company="Altemiq">
// Copyright (c) Altemiq. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Altemiq.Assembly.ChangeDetection.Query
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using Mono.Cecil;

    /// <summary>
    /// The event query.
    /// </summary>
    internal class EventQuery : MethodQuery
    {
        private readonly string? eventTypeFilter;

        /// <summary>
        /// Initialises a new instance of the <see cref="EventQuery"/> class.
        /// </summary>
        public EventQuery()
            : this("*")
        {
        }

        /// <summary>
        /// Initialises a new instance of the <see cref="EventQuery"/> class.
        /// </summary>
        /// <param name="query">The query.</param>
        public EventQuery(string query)
            : base("*")
        {
            if (string.IsNullOrEmpty(query))
            {
                throw new ArgumentNullException(nameof(query));
            }

            if (query == "*")
            {
                return;
            }

            // Get cached regex
            this.Parser = EventQueryParser;

            var match = this.Parser.Match(query);
            if (!match.Success)
            {
                throw new ArgumentException(string.Format(Properties.Resources.Culture, "The event query string {0} was not a valid query.", query), nameof(query));
            }

            this.SetModifierFilter(match);
            this.eventTypeFilter = PrependStarBeforeGenericTypes(GenericTypeMapper.ConvertClrTypeName(GetValue(match, "eventType")));

            if (!this.eventTypeFilter.StartsWith("*", StringComparison.Ordinal))
            {
                this.eventTypeFilter = "*" + this.eventTypeFilter;
            }

            if (this.eventTypeFilter == "*")
            {
                this.eventTypeFilter = default;
            }

            this.NameFilter = GetValue(match, "eventName");
        }

        /// <summary>
        /// Gets the public events.
        /// </summary>
        public static EventQuery PublicEvents { get; } = new EventQuery("public * *");

        /// <summary>
        /// Gets the protected events.
        /// </summary>
        public static EventQuery ProtectedEvents { get; } = new EventQuery("protected * *");

        /// <summary>
        /// Gets the internal events.
        /// </summary>
        public static EventQuery InternalEvents { get; } = new EventQuery("internal * *");

        /// <summary>
        /// Gets all the events.
        /// </summary>
        public static EventQuery AllEvents { get; } = new EventQuery("* *");

        /// <inheritdoc/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override IList<MethodDefinition> GetMethods(TypeDefinition type) => throw new NotSupportedException(Properties.Resources.NoEventQueryMethodMatch);

        /// <summary>
        /// Gets the matching events.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The matching events.</returns>
        public IList<EventDefinition> GetMatchingEvents(TypeDefinition type)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return type.Events.Where(this.IsMatchingEvent).ToArray();
        }

        private static string PrependStarBeforeGenericTypes(string eventTypeFilter) => eventTypeFilter.Replace("<", "<*").Replace("**", "*");

        private bool IsMatchingEvent(EventDefinition ev) => this.MatchMethodModifiers(ev.AddMethod) && this.MatchName(ev.Name) && this.MatchEventType(ev.EventType);

        private bool MatchEventType(TypeReference eventType) => string.IsNullOrEmpty(this.eventTypeFilter)
            || this.eventTypeFilter == "*"
            || Matcher.MatchWithWildcards(this.eventTypeFilter!, eventType.FullName, StringComparison.OrdinalIgnoreCase);
    }
}