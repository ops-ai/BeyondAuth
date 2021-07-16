using OpenTelemetry;
using System;
using System.Diagnostics;

namespace Identity.Core
{
    public class OpenTelemetryFilteredProcessor : BaseProcessor<Activity>
    {
        private readonly Func<Activity, bool> filter;
        private readonly BaseProcessor<Activity> processor;

        public OpenTelemetryFilteredProcessor(BaseProcessor<Activity> processor, Func<Activity, bool> filter)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            if (processor == null)
            {
                throw new ArgumentNullException(nameof(processor));
            }

            this.filter = filter;
            this.processor = processor;
        }

        public override void OnEnd(Activity activity)
        {
            // Call the underlying processor
            // only if the Filter returns true.
            if (this.filter(activity))
            {
                this.processor.OnEnd(activity);
            }
        }
    }
}