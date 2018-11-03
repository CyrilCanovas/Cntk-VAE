using ModelWrapper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Egnx.Tools;

namespace ModelTester
{

    class Program
    {
        static SortedList<Interval<float>, List<float>> Compact(SortedList<Interval<float>, List<float>> invect, int maxintervals)
        {
            var result = new SortedList<Interval<float>, List<float>>();
            var linvect = invect.OrderBy(x => x.Key).ToArray();
            var ratio = (double)linvect.Count() / (double)maxintervals;
            Interval<float> interval;
            for (int i = 0; i < maxintervals; i++)
            {
                var st_idx = (int)(i * ratio);
                var ed_idx = (int)((i + 1) * ratio);

                if (ed_idx >= linvect.Count())
                {
                    ed_idx = linvect.Count() - 1;
                }

                var leftitem = linvect[st_idx];
                var rightitem = linvect[ed_idx];

                if (i == 0)
                {
                    interval = new Interval<float>(leftitem.Key.Begin, rightitem.Key.End, true, false);
                }
                else if (i == (maxintervals - 1))
                {
                    interval = new Interval<float>(leftitem.Key.Begin, rightitem.Key.End, true, true);
                }
                else
                {
                    interval = new Interval<float>(leftitem.Key.Begin, rightitem.Key.End, true, false);
                }
                result.Add(interval, new List<float>());
            }
            var altresult = result.OrderBy(x => x.Key).ToArray();
            var allmeasures = invect.Select(x => x.Value).SelectMany(x => x).ToList();

            foreach (var measure in allmeasures)
            {
                List<float> wheretoadd = null;
                if (!altresult.Find(measure,0, altresult.Length-1,ref wheretoadd))
                {
                    throw new Exception("Error");
                }
                wheretoadd.Add(measure);
            }
            
            return result;
        }
        static void Main(string[] args)
        {
            var testfile = File.ReadAllLines(@"pictureset_training.ctf");

            var inputs = new List<float[]>();

            testfile.ToList().ForEach(x =>
            {
                var fields = x.Split(new string[] { "|" }, StringSplitOptions.None);
                if (fields.Length == 0) return;
                inputs.Add(
                    fields[1].Split(new string[] { " " }, StringSplitOptions.None).Skip(1).Select(y => XmlConvert.ToSingle(y)).ToArray()
                );
            }
            );

            var imagesetaewrapper = new SimpleModelWrapper(@"binancevae.dnn");

            var result = imagesetaewrapper.EvaluateModel(inputs);


            var invect = inputs.SelectMany(x => x.Select(y => y)).ToArray();
            var outvect = result.SelectMany(x => x.Select(y =>y)).ToArray();

            var diff = new float[invect.Length];

            for (int i = 0; i < diff.Length; i++)
            {
                var inval = invect[i];

                var ldiff = (inval - outvect[i]);
                //if (inval==0.0)
                //{
                //    if (outvect[i] != 0)
                //    {
                //        ldiff /= outvect[i];
                //    }
                //}
                //else
                //{
                //    ldiff /= inval;
                //}
                diff[i] = ldiff;

            }


            var rowcount = 4 * 20;  // 4 weeks , 20 months
            var colcount = 24 * 7;  // 24 hours , 7 days

            var recordwith = 7;
            var recordheight = 129;

            invect.Select(x=>(byte)(x * 256f)).ToArray()
                .Transform(recordwith, recordheight, colcount, rowcount)
                .ArrayToBitmap(@"input.bmp",
                    colcount * recordwith,
                    rowcount * recordheight,
                    System.Drawing.Imaging.PixelFormat.Format8bppIndexed

                );

            outvect.Select(x => (byte)(x * 256f)).ToArray()
                .Transform(recordwith, recordheight, colcount, rowcount)
                .ArrayToBitmap(@"out.bmp",
                    colcount * recordwith,
                    rowcount * recordheight,
                    System.Drawing.Imaging.PixelFormat.Format8bppIndexed

                );


            diff.Select(x => (byte)(Math.Abs(x) * 256f)).ToArray()
                .Transform(recordwith, recordheight, colcount, rowcount)
                .ArrayToBitmap(@"diff.bmp",
                    colcount * recordwith,
                    rowcount * recordheight,
                    System.Drawing.Imaging.PixelFormat.Format8bppIndexed
                );
            
            var sortedlist = new SortedList<Interval<float>, List<float>>();
            var maxintervals = 1024;
            var str = new StringBuilder();

            for (int i=0;i<maxintervals;i++)
            {
                var intervalp = (float) 1 *(i - (maxintervals >> 1)) / (float)(maxintervals >> 1);
                var intervaln = (float) 1 *((i+1) - (maxintervals >> 1)) / (float)(maxintervals >> 1);
                    sortedlist.Add(
                        new Interval<float>(intervalp, intervaln, true, false),
                        new List<float>());
            }

            var altresult = sortedlist.ToArray();

            foreach (var elt in diff)
            {
                List<float> wheretoadd = null;
                if (!altresult.Find(elt, 0, altresult.Length-1, ref wheretoadd))
                {
                    //throw new Exception("Error");
                }
                else
                {
                    wheretoadd.Add(elt);
                }
                
            }

            foreach (var x in sortedlist)
            {
                var values = new string[] { x.Key.Begin.ToString(), x.Key.End.ToString(), x.Value.Count().ToString() };
                str.AppendLine(string.Join("\t", values));
            }

            File.WriteAllText(@"diff.txt", str.ToString());
        }
    }
}
