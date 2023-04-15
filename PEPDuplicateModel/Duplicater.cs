using PEPExtensions;
using PEPlugin;
using PEPlugin.Pmx;
using PEPlugin.SDX;
using System;
using System.Linq;

namespace PEPDuplicateModel
{
    public class Duplicater
    {
        private IPERunArgs Args { get; }

        public Duplicater(IPERunArgs args)
        {
            Args = args;
        }

        public void Update(IPXPmx model)
        {
            Utility.Update(Args.Host.Connector, model);
        }

        public IPXPmx Duplicate(int count)
        {
            var pmx = Args.Host.Connector.Pmx.GetCurrentState();

            if (count < 2)
            {
                return pmx;
            }

            var rootsNode = PEStaticBuilder.Pmx.Node();
            rootsNode.Name = pmx.RootNode.Items.FirstOrDefault()?.BoneItem?.Bone?.Name ?? "すべての親";
            rootsNode.NameE = pmx.RootNode.Items.FirstOrDefault()?.BoneItem?.Bone?.NameE ?? "Sub Root";

            for (int duplicateNum = 1; duplicateNum < count; duplicateNum++)
            {
                var numSuffix = $" |{duplicateNum}";
                var offset = MakeOffset(duplicateNum);

                var duplication = (IPXPmx)pmx.Clone();

                foreach (var item in duplication.Body)
                {
                    item.Name += numSuffix;
                    item.NameE += numSuffix;
                    item.Position += offset;
                    pmx.Body.Add(item);
                }

                foreach (var item in duplication.Bone)
                {
                    item.Name += numSuffix;
                    item.NameE += numSuffix;
                    item.Position += offset;
                    pmx.Bone.Add(item);
                }

                foreach (var item in duplication.ExpressionNode.Items)
                {
                    pmx.ExpressionNode.Items.Add(item);
                }

                foreach (var item in duplication.Joint)
                {
                    item.Name += numSuffix;
                    item.NameE += numSuffix;
                    item.Position += offset;
                    pmx.Joint.Add(item);
                }

                foreach (var item in duplication.Material)
                {
                    item.Name += numSuffix;
                    item.NameE += numSuffix;
                    pmx.Material.Add(item);
                }

                foreach (var item in duplication.Morph)
                {
                    item.Name += numSuffix;
                    item.NameE += numSuffix;
                    pmx.Morph.Add(item);
                }

                foreach (var (item, nodeId) in duplication.Node.Select((node, id) => (node, id)))
                {
                    item.Name += numSuffix;
                    item.NameE += numSuffix;
                    pmx.Node.Insert(nodeId * duplicateNum + nodeId + duplicateNum, item);
                }

                foreach (var item in duplication.Vertex)
                {
                    item.Position += offset;
                    pmx.Vertex.Add(item);
                }

                rootsNode.Items.Add(duplication.RootNode.Items.FirstOrDefault());
            }

            pmx.Node.Insert(0, rootsNode);

            return pmx;
        }

        private static V3 MakeOffset(int num)
        {
            var quot = Math.DivRem(num - 1, 4, out var directionNum);
            var scale = 5.0f * (quot + 1);

            switch (directionNum)
            {
                case 0:
                    return new V3(scale, 0, 0);
                case 1:
                    return new V3(-scale, 0, 0);
                case 2:
                    return new V3(0, 0, scale);
                case 3:
                    return new V3(0, 0, -scale);
                default:
                    return new V3();
            }
        }
    }
}
