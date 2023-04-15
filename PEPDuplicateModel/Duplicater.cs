﻿using PEPExtensions;
using PEPlugin;
using PEPlugin.Pmx;
using PEPlugin.SDX;
using SlimDX;
using System;
using System.Linq;
using System.Text.RegularExpressions;

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
            var prototype = (IPXPmx)pmx.Clone();

            if (count < 2)
            {
                return pmx;
            }

            var rootsNode = PEStaticBuilder.Pmx.Node();
            rootsNode.Name = pmx.RootNode.Items.FirstOrDefault()?.BoneItem?.Bone?.Name ?? "すべての親";
            rootsNode.NameE = pmx.RootNode.Items.FirstOrDefault()?.BoneItem?.Bone?.NameE ?? "Sub Root";

            foreach (var duplicant in Enumerable.Range(1, count - 1).Select(i => CreateDuplicant(pmx, prototype, i, setLocalAxisToArmBones)))
            {
                rootsNode.Items.Add(duplicant.RootNode.Items.FirstOrDefault());
            }

            pmx.Node.Insert(0, rootsNode);

            if (addAllParent)
            {
                // 元の全親を新しい表情枠に追加
                rootsNode.Items.Insert(0, pmx.RootNode.Items.FirstOrDefault());

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

        private static IPXPmx CreateDuplicant(IPXPmx pmx, IPXPmx prototype, int duplicationIndex, bool setLocalAxisToArmBones)
        {
            var duplicant = (IPXPmx)prototype.Clone();

            if (setLocalAxisToArmBones)
            {
                string[] armBoneNames = new[]
                {
                    "腕",
                    "ひじ",
                    "手首",
                };

                foreach (var prefix in new[] { "右", "左" })
                {
                    var armBones = FindArmBones(duplicant, prefix, armBoneNames);

                    V3 armDirection = CalcDirection(armBones.Arm, armBones.Elbow);
                    V3 elbowDirection = CalcDirection(armBones.Elbow, armBones.Wrist);
                    V3 wristDirection = armBones.Wrist.ToBone is null ? armBones.Wrist.ToOffset : CalcDirection(armBones.Wrist, armBones.Wrist.ToBone);

                    var targetBones = armBoneNames
                        .Zip(new[] { armDirection, elbowDirection, wristDirection }, (name, dir) => (Name: prefix + name, Direction: dir))
                        .SelectMany(bone =>
                            duplicant.Bone.Where(b => Regex.IsMatch(b.Name, bone.Name + @"親?[0-9０-９]*$")).Select(b => (Bone: b, bone.Direction))
                        );
                    foreach (var (bone, direction) in targetBones)
                    {
                        SetLocalAxis(bone, direction);
                    }
                }
            }

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
                var originalNodeIndex = pmx.Node.IndexOf(pmx.Node.First(node => node.Name == item.Name));
                int insertIndex = originalNodeIndex + duplicationIndex;

                item.Name += numSuffix;
                item.NameE += numSuffix;

                if (insertIndex < pmx.Node.Count)
                    pmx.Node.Insert(insertIndex, item);
                else
                    pmx.Node.Add(item);
            }

            foreach (var item in duplicant.Vertex)
            {
                item.Position += offset;
                pmx.Vertex.Add(item);
            }

            return duplicant;
        }

        private static (IPXBone Arm, IPXBone Elbow, IPXBone Wrist) FindArmBones(IPXPmx pmx, string prefix, string[] armBoneNames)
        {
            var bones = armBoneNames.Select(name => prefix + name).Select(name => pmx.Bone.FirstOrDefault(bone => bone.Name == name)).ToArray();

            return (bones[0], bones[1], bones[2]);
        }

        private static void SetLocalAxis(IPXBone bone, Vector3 direction)
        {
            if (direction == Vector3.Zero) { return; }

            direction.Normalize();

            if (direction == Vector3.UnitZ)
            {
                bone.SetLocalAxis(Vector3.UnitZ, -Vector3.UnitX);
            }
            else if (direction == -Vector3.UnitZ)
            {
                bone.SetLocalAxis(-Vector3.UnitZ, Vector3.UnitX);
            }
            else
            {
                Matrix matrix = Matrix.RotationQuaternion(Q.Dir(direction, Vector3.UnitZ, Vector3.UnitX, Vector3.UnitZ));
                bone.SetLocalAxis(Vector3.TransformNormal(Vector3.UnitX, matrix), Vector3.TransformNormal(Vector3.UnitZ, matrix));
            }

            bone.IsLocalFrame = true;
        }

        private static V3 CalcDirection(IPXBone source, IPXBone destination) => destination.Position - source.Position;

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
