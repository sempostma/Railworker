using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RWLib.Tracks
{

    // Not thread-safe
    public class GeoJsonStreamWriter
    {
        private StreamWriter streamWriter;
        private GeoJsonAdapter geoJsonAdapter;
        private bool wroteHeader = false;

        public GeoJsonStreamWriter(Stream destinationStream, GeoJsonAdapter geoJsonAdapter)
        {
            streamWriter = new StreamWriter(destinationStream);
            this.geoJsonAdapter = geoJsonAdapter;
        }

        public async Task Write(GeoJsonAdapter.Feature feature)
        {
            if (wroteHeader == false)
            {
                wroteHeader = true;
                await streamWriter.WriteAsync(geoJsonAdapter.FeatureCollectionHeader);
                await streamWriter.FlushAsync();
            } else
            {
                await streamWriter.WriteAsync("," + geoJsonAdapter.options.NewLine);
                await streamWriter.FlushAsync();
            }
            await JsonSerializer.SerializeAsync(streamWriter.BaseStream, feature, geoJsonAdapter.options.serializerOptions);
        }

        public async Task Finish()
        {
            await streamWriter.WriteAsync(geoJsonAdapter.FeatureCollectionFooter);
            await streamWriter.DisposeAsync();
        }
    }
}
