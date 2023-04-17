// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Packages/com.meta.utilities.watch-window/LICENSE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Meta.Utilities.WatchWindow
{
    public static class Extensions
    {
        public static string ListToString<T>(this IEnumerable<T> values, string separator = ", ") => string.Join(separator, values);

        public static IEnumerable<(A, B)> Zip<A, B>(this IEnumerable<A> a, IEnumerable<B> b)
        {
            var ea = a.GetEnumerator();
            var eb = b.GetEnumerator();
            while (ea.MoveNext() && eb.MoveNext())
                yield return (ea.Current, eb.Current);
        }

        public static decimal? AsDecimal(this object obj) => obj switch
        {
            bool boolVal => boolVal ? 1m : 0m,
            byte byteVal => byteVal,
            sbyte sbyteVal => sbyteVal,
            ushort uint16Val => uint16Val,
            uint uint32Val => uint32Val,
            ulong uint64Val => uint64Val,
            short int16Val => int16Val,
            int int32Val => int32Val,
            long int64Val => int64Val,
            decimal decimalVal => decimalVal,
            double doubleVal => (decimal)doubleVal,
            float singleVal => (decimal)singleVal,
            Enum => (int)obj,
            _ => null,
        };

        public static object GetPrivateMember(this object obj, string memberName)
        {
            var member = obj?.GetType()?.GetMember(memberName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)?.FirstOrDefault();
            return member switch
            {
                FieldInfo field => field.GetValue(obj),
                PropertyInfo prop => prop.GetValue(obj),
                null => throw new Exception($"Could not find {memberName}"),
                _ => throw new Exception($"{memberName} is not a field or property"),
            };
        }

        public static R GetPrivateMember<R>(this object obj, string memberName) => (R)obj.GetPrivateMember(memberName);

        public static void SetPrivateMember(this object obj, string memberName, object value)
        {
            var member = obj?.GetType()?.GetMember(memberName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)?.FirstOrDefault();
            switch (member)
            {
                case FieldInfo field:
                    field.SetValue(obj, value);
                    break;
                case PropertyInfo prop:
                    prop.SetValue(obj, value);
                    break;
                case null:
                    throw new Exception($"Could not find {memberName}");
                default:
                    throw new Exception($"{memberName} is not a field or property");
            }
        }

        public static GlobalObjectId? ParseGlobalObjectId(this string str)
        {
            // TryParse fails on null GUIDs, so do it ourselves... thanks Unity
            if (GlobalObjectId.TryParse(str ?? "", out var parsed))
                return parsed;

            var id = (object)new GlobalObjectId();

            var tokens = str.Split('-');
            if (tokens.Length != 5 || tokens[0] != "GlobalObjectId_V1")
                return null;

            if (!int.TryParse(tokens[1], out var identifierType) ||
                tokens[2].Length != 32 ||
                !ulong.TryParse(tokens[3], out var targetObject) ||
                !ulong.TryParse(tokens[4], out var targetPrefab))
                return null;

            var assetGuid = new GUID(tokens[2]);
            id.SetPrivateMember("m_IdentifierType", identifierType);
            id.SetPrivateMember("m_AssetGUID", assetGuid);

            var sceneObject = Activator.CreateInstance(typeof(GlobalObjectId).Assembly.GetType("UnityEditor.SceneObjectIdentifier"));
            sceneObject.SetPrivateMember("TargetObject", targetObject);
            sceneObject.SetPrivateMember("TargetPrefab", targetPrefab);
            id.SetPrivateMember("m_SceneObjectIdentifier", sceneObject);

            return id as GlobalObjectId?;
        }

        [Serializable]
        public struct ScenePathNode
        {
            public enum Type
            {
                GameObject,
                Component,
                Scene
            }
            public Type m_type;
            public string m_name;
            public int m_index;
        }

        public static IEnumerable<ScenePathNode> GetScenePath(this Scene scene)
        {
            return new ScenePathNode[]
            {
                new()
                {
                    m_type = ScenePathNode.Type.Scene,
                    m_name = scene.name,
                }
            };
        }

        public static IEnumerable<ScenePathNode> GetScenePath(this UnityEngine.Object obj)
        {
            var parentNodes = obj switch
            {
                GameObject gameObject => gameObject.transform.parent is { } parent ? parent.gameObject.GetScenePath() : gameObject.scene.GetScenePath(),
                Component component => component.gameObject.GetScenePath(),
                _ => throw new Exception($"Unknown object in scene path with type {obj?.GetType()?.FullName ?? "null"}"),
            };
            return parentNodes.Append(obj switch
            {
                GameObject gameObject => new ScenePathNode()
                {
                    m_name = gameObject.name,
                    m_index = gameObject.transform.GetSiblingIndex(),
                    m_type = ScenePathNode.Type.GameObject,
                },
                Component component => new ScenePathNode()
                {
                    m_name = component.GetType().FullName,
                    m_index = Array.IndexOf(component.gameObject.GetComponents(component.GetType()), component),
                    m_type = ScenePathNode.Type.Component,
                },
                _ => throw new Exception($"Unknown object in scene path with type {obj?.GetType()?.FullName ?? "null"}"),
            });
        }

        public static UnityEngine.Object GetObject(this IEnumerable<ScenePathNode> nodes)
        {
            if (nodes is null)
                return null;

            var node = nodes.GetEnumerator();
            if (!node.MoveNext() || node.Current.m_type is not ScenePathNode.Type.Scene)
                return null;

            var scene = SceneManager.GetSceneByName(node.Current.m_name);
            var obj = null as GameObject;
            while (node.MoveNext())
            {
                if (node.Current.m_type is ScenePathNode.Type.GameObject)
                {
                    GameObject TryGetObject(GameObject[] objs)
                    {
                        return objs.Length != 0 && objs.Length > node.Current.m_index && objs[node.Current.m_index].name == node.Current.m_name
                            ? objs[node.Current.m_index]
                            : objs.FirstOrDefault(o => o.name == node.Current.m_name);
                    }

                    if (obj != null)
                    {
                        obj = TryGetObject(obj.transform.OfType<Transform>().Select(t => t.gameObject).ToArray());
                        continue;
                    }

                    if (obj == null && scene.isLoaded)
                    {
                        obj = TryGetObject(scene.GetRootGameObjects());
                    }

                    if (obj == null)
                    {
                        for (var i = 0; i != SceneManager.sceneCount; ++i)
                        {
                            obj = TryGetObject(SceneManager.GetSceneAt(i).GetRootGameObjects());
                            if (obj != null)
                                break;
                        }
                    }

                    if (obj == null && Application.isPlaying)
                    {
                        var dontDestroyObj = new GameObject("IGNORE_ME");
                        UnityEngine.Object.DontDestroyOnLoad(dontDestroyObj);
                        Debug.Log(dontDestroyObj.scene.name);
                        obj = TryGetObject(dontDestroyObj.scene.GetRootGameObjects());
                        UnityEngine.Object.DestroyImmediate(dontDestroyObj);
                    }
                }
                else if (node.Current.m_type is ScenePathNode.Type.Component)
                {
                    var comp = node.Current;
                    Assert.IsFalse(node.MoveNext(), "Components should only be last in ScenePaths");
                    var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(comp.m_name)).FirstOrDefault(t => t != null);
                    Assert.IsNotNull(type, $"What type is {comp.m_name}");
                    var components = obj.GetComponents(type);
                    return components.Length > comp.m_index ? components[comp.m_index] : null;
                }
                else
                {
                    Debug.LogError($"{node.Current.m_type} node shouldn't be here");
                }
            }
            return obj;
        }
    }
}
