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

        public IPXPmx Duplicate(int count, bool addAllParent, bool setLocalAxisToArmBones)
        {
            var pmx = Args.Host.Connector.Pmx.GetCurrentState();

            if (count < 2)
            {
                return pmx;
            }

            var rootsNode = PEStaticBuilder.Pmx.Node();
            rootsNode.Name = pmx.RootNode.Items.FirstOrDefault()?.BoneItem?.Bone?.Name ?? "すべての親";
            rootsNode.NameE = pmx.RootNode.Items.FirstOrDefault()?.BoneItem?.Bone?.NameE ?? "Sub Root";

            foreach (var duplicant in Enumerable.Range(1, count - 1).Select(i => CreateDuplicant(pmx, i)))
            {
                rootsNode.Items.Add(duplicant.RootNode.Items.FirstOrDefault());
            }

            pmx.Node.Insert(0, rootsNode);

            if (addAllParent)
            {
                // 元の全親を新しい表情枠に追加
                rootsNode.Items.Add(pmx.RootNode.Items.FirstOrDefault());

                // 全体親を追加
                var superRoot = Args.Host.Builder.Pmx.Bone();
                superRoot.Name = "全体親";
                superRoot.NameE = "Super Root";
                superRoot.IsRotation = true;
                superRoot.IsTranslation = true;

                // 既存の親なしボーンに全体親を親設定
                foreach (var bone in pmx.Bone.Where(bone => bone.Parent is null))
                {
                    bone.Parent = superRoot;
                }

                // root 表示枠に全体親を設定
                pmx.Bone.Insert(0, superRoot);
                pmx.RootNode.Items[0] = Args.Host.Builder.Pmx.BoneNodeItem(superRoot);
            }

            return pmx;
        }

        private static IPXPmx CreateDuplicant(IPXPmx pmx, int duplicationIndex)
        {
            var duplicant = (IPXPmx)pmx.Clone();

            var numSuffix = $" |{duplicationIndex}";
            var offset = MakeOffset(duplicationIndex);

            foreach (var item in duplicant.Body)
            {
                item.Name += numSuffix;
                item.NameE += numSuffix;
                item.Position += offset;
                pmx.Body.Add(item);
            }

            foreach (var item in duplicant.Bone)
            {
                item.Name += numSuffix;
                item.NameE += numSuffix;
                item.Position += offset;
                pmx.Bone.Add(item);
            }

            foreach (var item in duplicant.ExpressionNode.Items)
            {
                pmx.ExpressionNode.Items.Add(item);
            }

            foreach (var item in duplicant.Joint)
            {
                item.Name += numSuffix;
                item.NameE += numSuffix;
                item.Position += offset;
                pmx.Joint.Add(item);
            }

            foreach (var item in duplicant.Material)
            {
                item.Name += numSuffix;
                item.NameE += numSuffix;
                pmx.Material.Add(item);
            }

            foreach (var item in duplicant.Morph)
            {
                item.Name += numSuffix;
                item.NameE += numSuffix;
                pmx.Morph.Add(item);
            }

            foreach (var (item, nodeId) in duplicant.Node.Select((node, id) => (node, id)))
            {
                item.Name += numSuffix;
                item.NameE += numSuffix;
                pmx.Node.Insert(nodeId * duplicationIndex + nodeId + duplicationIndex, item);
            }

            foreach (var item in duplicant.Vertex)
            {
                item.Position += offset;
                pmx.Vertex.Add(item);
            }

            return duplicant;
        }

        private static V3 MakeOffset(int index)
        {
            var quot = Math.DivRem(index - 1, 4, out var directionNum);
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
