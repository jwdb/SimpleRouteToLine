using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Humanizer;
using Itinero;
using Itinero.IO.Osm;
using Itinero.Osm.Vehicles;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using OsmSharp.Streams;
using SkiaSharp;

namespace RoutingTest
{
    class Program
    {
        static void Main(string[] args)
        {

            // load some routing data and build a routing network.
            var routerDb = new RouterDb();
            using var stream = File.Open(@"C:\Users\yukik\Downloads\zuid-holland-latest.osm.pbf", FileMode.Open, FileAccess.Read);
            var src = new PBFOsmStreamSource(stream);
            routerDb.LoadOsmData(src, Vehicle.Bicycle); // create the network for Bicycle only.

            // create a router.
            var router = new Router(routerDb);

            // get a profile.
            var profile = Vehicle.Bicycle.Fastest(); // the default OSM Bicycle profile.

            // create a routerpoint from a location.
            // snaps the given location to the nearest routable edge.
            var start = router.Resolve(profile, 52.154820f, 4.479307f);
            var end = router.Resolve(profile, 52.160942f, 4.495821f);

            // calculate a route.
            var route = router.Calculate(profile, start, end);

            var retList = new List<(float lat, float lon, float time, float distance, string name)>();


            // Create the path
            var path = new SKPath();

            var firstShape = route.Shape[0];

            path.MoveTo(5, 5);

            var strokePaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = SKColors.Black,
                StrokeWidth = 1
            };

            int scale = 50000;

            for (int i = 1; i < route.ShapeMeta.Length - 2; i++)
            {
                var meta = route.ShapeMeta[i];

                var strn = meta.Attributes.TryGetValue("name", out var desc);
                var current = route.Shape[meta.Shape];
                if (meta.Shape > 0 && route.ShapeMeta[i - 1].Attributes.TryGetValue("name", out var v) && v == desc) continue;

                if (route.Shape.Length <= meta.Shape + 1) continue;
                var lat = (current.Latitude - firstShape.Latitude) * scale;
                var lon = (current.Longitude - firstShape.Longitude) * scale;
                retList.Add((lat, lon, meta.Time - retList.LastOrDefault().time, meta.Distance - retList.LastOrDefault().distance, desc));

                path.LineTo(lon, -lat);
            }

            var rect = path.Bounds;
            var translationMatrix = SKMatrix.CreateTranslation(Math.Abs(path.Bounds.Left) + 20, Math.Abs(path.Bounds.Top) + 20);
            path.Transform(translationMatrix);
            rect.Inflate(100, 10);
            using var outStream = new SKDynamicMemoryWStream();
            var svg = SKSvgCanvas.Create(rect, outStream);


            svg.DrawPath(path, strokePaint);

            foreach (var (lat, lon, time, distance, name) in retList)
            {
                var point = new SKPoint(lon, -lat);

                svg.DrawText($"{name} ({TimeSpan.FromSeconds(time).Humanize()} - {Math.Round(distance)} meter) ", translationMatrix.MapPoint(point), strokePaint);
            }


            var s = outStream.DetachAsData();
            File.WriteAllBytes("test.svg", s.ToArray());

            Console.WriteLine("Hello World!");
        }
    }
}
