using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DateDetectorV2.Dater
{
    public class DateResult
    {
        public DateResult() {
            Format = new Dictionary<string, int>();
        }
        [JsonConverter(typeof(StringEnumConverter))]
        public Status Status { get; set; }

        public string Value { get; set; }

        public int StartIndex { get; set; }

        public int EndIndex { get; set; }

        public Dictionary<string, int> Format { get; }

        public override bool Equals(Object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }
            else
            {
                DateResult dr = (DateResult)obj;
                return (Value.Equals(dr.Value)
                    && StartIndex == dr.StartIndex
                    && EndIndex == dr.EndIndex);
            }
        }

        // TODO
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
