using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Gilzoide.GetComponentContextMenu.Editor
{
    public static class GetComponentContextMenu
    {
        private static readonly GUIContent _getComponentTitle = new GUIContent("GetComponent");
        private static readonly GUIContent _getComponentInChildrenTitle = new GUIContent("GetComponentInChildren");
        private static readonly GUIContent _getComponentInParentTitle = new GUIContent("GetComponentInParent");
        private static readonly GUIContent _findObjectOfTypeTitle = new GUIContent("FindObjectOfType");

        private static readonly GUIContent _getComponentsTitle = new GUIContent("GetComponents");
        private static readonly GUIContent _getComponentsInChildrenTitle = new GUIContent("GetComponentsInChildren");
        private static readonly GUIContent _getComponentsInParentTitle = new GUIContent("GetComponentsInParent");
        private static readonly GUIContent _findObjectsOfTypeTitle = new GUIContent("FindObjectsOfType");

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.contextualPropertyMenu += OnPropertyContextMenu;
        }

        public static void OnPropertyContextMenu(GenericMenu menu, SerializedProperty property)
        {
            Object[] targetObjects = property.serializedObject.targetObjects;
            if (targetObjects.Length != 1 || !(targetObjects[0] is Component component))
            {
                return;
            }

            if (property.propertyType == SerializedPropertyType.ObjectReference
                && TryFindObjectType(property.type, out Type objectType)
                && objectType.IsSubclassOf(typeof(Component)))
            {
                SerializedProperty currentProperty = property.Copy();
                menu.AddItem(_getComponentTitle, false, () =>
                {
                    currentProperty.objectReferenceValue = component.GetComponent(objectType);
                    currentProperty.serializedObject.ApplyModifiedProperties();
                });

                menu.AddItem(_getComponentInChildrenTitle, false, () =>
                {
                    currentProperty.objectReferenceValue = component.GetComponentInChildren(objectType, true);
                    currentProperty.serializedObject.ApplyModifiedProperties();
                });

                menu.AddItem(_getComponentInParentTitle, false, () =>
                {
                    currentProperty.objectReferenceValue = component.GetComponentInParent(objectType, true);
                    currentProperty.serializedObject.ApplyModifiedProperties();
                });

                menu.AddItem(_findObjectOfTypeTitle, false, () =>
                {
                    currentProperty.objectReferenceValue = Object.FindObjectOfType(objectType, true);
                    currentProperty.serializedObject.ApplyModifiedProperties();
                });

                menu.AddSeparator("");
            }
            else if (property.isArray
                && TryFindObjectType(property.arrayElementType, out objectType)
                && objectType.IsSubclassOf(typeof(Component)))
            {
                SerializedProperty currentProperty = property.Copy();
                menu.AddItem(_getComponentsTitle, false, () =>
                {
                    currentProperty.ClearArray();
                    currentProperty.InsertObjectsInArray(component.GetComponents(objectType));
                    currentProperty.serializedObject.ApplyModifiedProperties();
                });

                menu.AddItem(_getComponentsInChildrenTitle, false, () =>
                {
                    currentProperty.ClearArray();
                    currentProperty.InsertObjectsInArray(component.GetComponentsInChildren(objectType, true));
                    currentProperty.serializedObject.ApplyModifiedProperties();
                });

                menu.AddItem(_getComponentsInParentTitle, false, () =>
                {
                    currentProperty.ClearArray();
                    currentProperty.InsertObjectsInArray(component.GetComponentsInParent(objectType, true));
                    currentProperty.serializedObject.ApplyModifiedProperties();
                });

                menu.AddItem(_findObjectsOfTypeTitle, false, () =>
                {
                    currentProperty.ClearArray();
                    currentProperty.InsertObjectsInArray(Object.FindObjectsOfType(objectType, true));
                    currentProperty.serializedObject.ApplyModifiedProperties();
                });

                menu.AddSeparator("");
            }
        }

        #region Helper methods

        public static void InsertObjectsInArray(this SerializedProperty property, IReadOnlyList<Object> objects)
        {
            int previousArraySize = property.arraySize;
            for (int i = 0; i < objects.Count; i++)
            {
                property.InsertArrayElementAtIndex(i + previousArraySize);
                property.GetArrayElementAtIndex(i + previousArraySize).objectReferenceValue = objects[i];
            }
        }

        public static bool TryFindObjectType(string propertyTypeString, out Type type)
        {
            Match typeNameMatch = _propertyTypeRegex.Match(propertyTypeString);
            if (typeNameMatch.Success)
            {
                string typeName = typeNameMatch.Groups[1].Value;
                type = typeName == nameof(Object)
                    ? typeof(Object)
                    : ObjectSubclasses.First(type => type.Name == typeName);
                return true;
            }
            else
            {
                type = null;
                return false;
            }
        }

        private static IList<Type> ObjectSubclasses => _objectSubclasses != null ? _objectSubclasses : (_objectSubclasses = FindObjectSubclasses());
        private static IList<Type> _objectSubclasses;
        private static readonly Regex _propertyTypeRegex = new Regex(@"\s*PPtr\W*(\w+)");

        private static IList<Type> FindObjectSubclasses()
        {
#if UNITY_2019_2_OR_NEWER
            return TypeCache.GetTypesDerivedFrom<Object>();
#else
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(asm => asm.GetTypes())
                .Where(type => type.IsSubclassOf(typeof(Object)))
                .ToList();
#endif
        }

        #endregion
    }
}
