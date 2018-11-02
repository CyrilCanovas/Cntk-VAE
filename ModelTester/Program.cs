using ModelWrapper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ModelTester
{

    class Program
    {
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
                diff[i] = outvect[i]-invect[i];
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

            var str = new StringBuilder();
            foreach (var elt in diff)
            {
                str.AppendLine(string.Join("\t", elt));
            }

            File.WriteAllText(@"diff.txt", str.ToString());
        }
    }
}
