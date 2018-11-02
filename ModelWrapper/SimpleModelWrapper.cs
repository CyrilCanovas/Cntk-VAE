using CNTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ModelWrapper
{
    public class SimpleModelWrapper
    {
        private readonly Function model;
        private readonly Variable model_input;
        private readonly NDShape model_input_shape;
        private readonly DeviceDescriptor device = DeviceDescriptor.CPUDevice;
        public SimpleModelWrapper(string model_filename, DeviceDescriptor device = null)
        {
            if (device != null)
            {
                this.device = device;
            }
            this.model = Function.Load(model_filename, this.device);
            this.model_input = model.Arguments[0];
            this.model_input_shape = this.model_input.Shape;
        }


        public IEnumerable<float[]> EvaluateModel(IEnumerable<float[]> values_set)
        {
            var result = new List<float[]>();

            Dictionary<Variable, Value> inputDataMap = new Dictionary<Variable, Value>() { { model_input, null } };
            Dictionary<Variable, Value> outputDataMap = new Dictionary<Variable, Value>() { { model.Output, null } };
            foreach (var values in values_set)
            {
                var ndarrayview = new NDArrayView(model_input_shape, values, device);
                var value = new Value(ndarrayview);
                inputDataMap[model_input] = value;
                outputDataMap[model.Output] = null;
                model.Evaluate(inputDataMap, outputDataMap, device);
                var outputValue = outputDataMap[model.Output];
                result.Add(outputValue.GetDenseData<float>(model.Output).Single().ToArray());
            }

            return result;
        }

    }
}
