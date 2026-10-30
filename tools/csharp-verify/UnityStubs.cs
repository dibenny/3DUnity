// ============================================================================
// Unity API 桩（Stub）
//
// 用途：本机没有 Unity，无法用真正的 UnityEngine.dll 编译。
// 这里手写一份覆盖本项目用到的 Unity API 签名，让 Roslyn 能对
// unity/InspectionDemo/Assets/Scripts 下的 C# 代码做一次真正的编译检查。
//
// 重要限制（必须知道）：
//   桩是我手写的，不代表 Unity 真实 API。
//   编译通过 ≠ 在 Unity 里能跑。只保证「代码自身没有拼写/签名/类型错误」。
//   Unity 真实 API 的存在性和行为，仍需在真机 Editor 里确认。
// ============================================================================

using System;
using System.Collections;
using System.Collections.Generic;

// ---------------------------------------------------------------- UnityEngine
namespace UnityEngine
{
    public class Object
    {
        public string name { get; set; }
        public static void Destroy(Object obj) { }
        public static void DestroyImmediate(Object obj) { }
        public static T Instantiate<T>(T original) where T : Object { return original; }
        public static T[] FindObjectsOfType<T>() where T : Object { return new T[0]; }
        public static T FindObjectOfType<T>() where T : Object { return null; }
        public static bool operator ==(Object a, Object b) { return ReferenceEquals(a, b); }
        public static bool operator !=(Object a, Object b) { return !ReferenceEquals(a, b); }
        public override bool Equals(object other) { return ReferenceEquals(this, other); }
        public override int GetHashCode() { return base.GetHashCode(); }
    }

    public class Component : Object
    {
        public GameObject gameObject { get; }
        public Transform transform { get; }
        public T GetComponent<T>() { return default(T); }
        public T GetComponentInParent<T>() { return default(T); }
    }

    public class Behaviour : Component
    {
        public bool enabled { get; set; }
        public bool isActiveAndEnabled { get { return true; } }
    }

    public class Coroutine : Object { }
    public class YieldInstruction { }
    public class WaitForSeconds : YieldInstruction { public WaitForSeconds(float seconds) { } }

    public class MonoBehaviour : Behaviour
    {
        public Coroutine StartCoroutine(IEnumerator routine) { return null; }
        public void StopCoroutine(IEnumerator routine) { }
        public void StopCoroutine(Coroutine routine) { }
        public void StopAllCoroutines() { }
        public void Invoke(string methodName, float time) { }
        public void CancelInvoke() { }
    }

    public class ScriptableObject : Object { }

    public class GameObject : Object
    {
        public GameObject() { }
        public GameObject(string name) { }
        public GameObject(string name, params Type[] components) { }
        public Transform transform { get; }
        public string tag { get; set; }
        public bool activeSelf { get { return true; } }
        public bool activeInHierarchy { get { return true; } }
        public int layer { get; set; }
        public void SetActive(bool value) { }
        public T AddComponent<T>() where T : Component, new() { return new T(); }
        public T GetComponent<T>() { return default(T); }
        public static GameObject CreatePrimitive(PrimitiveType type) { return new GameObject(); }
        public static GameObject Find(string name) { return null; }
    }

    public class Transform : Component, IEnumerable
    {
        public Vector3 position { get; set; }
        public Vector3 localPosition { get; set; }
        public Quaternion rotation { get; set; }
        public Quaternion localRotation { get; set; }
        public Vector3 localScale { get; set; }
        public Vector3 eulerAngles { get; set; }
        public Vector3 forward { get; set; }
        public Vector3 right { get; set; }
        public Vector3 up { get; set; }
        public Transform parent { get; set; }
        public int childCount { get { return 0; } }
        public void SetParent(Transform p, bool worldPositionStays = true) { }
        public void SetParent(Transform p) { }
        public void Rotate(Vector3 eulers) { }
        public void Rotate(Vector3 eulers, Space space) { }
        public void Rotate(float x, float y, float z) { }
        public void Rotate(float x, float y, float z, Space space) { }
        public Transform GetChild(int index) { return null; }
        public IEnumerator GetEnumerator() { yield break; }
    }

    public class RectTransform : Transform
    {
        public Vector2 anchorMin { get; set; }
        public Vector2 anchorMax { get; set; }
        public Vector2 pivot { get; set; }
        public Vector2 anchoredPosition { get; set; }
        public Vector2 sizeDelta { get; set; }
        public Vector2 offsetMin { get; set; }
        public Vector2 offsetMax { get; set; }
    }

    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero { get { return new Vector2(0, 0); } }
        public static Vector2 one { get { return new Vector2(1, 1); } }
        // 真实 Unity 里 Vector2 -> Vector3 有隐式转换
        public static implicit operator Vector3(Vector2 v) { return new Vector3(v.x, v.y, 0f); }
    }

    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero { get { return new Vector3(0, 0, 0); } }
        public static Vector3 one { get { return new Vector3(1, 1, 1); } }
        public static Vector3 up { get { return new Vector3(0, 1, 0); } }
        public static Vector3 down { get { return new Vector3(0, -1, 0); } }
        public static Vector3 forward { get { return new Vector3(0, 0, 1); } }
        public Vector3 normalized { get { return this; } }
        public float magnitude { get { return 0f; } }
        public float sqrMagnitude { get { return 0f; } }
        public static Vector3 operator +(Vector3 a, Vector3 b) { return a; }
        public static Vector3 operator -(Vector3 a, Vector3 b) { return a; }
        public static Vector3 operator *(Vector3 a, float b) { return a; }
        public static Vector3 operator *(float a, Vector3 b) { return b; }
        public static Vector3 operator /(Vector3 a, float b) { return a; }
        // 真实 Unity 里 Vector3 <-> Vector2 有隐式转换，桩里必须补上
        public static implicit operator Vector2(Vector3 v) { return new Vector2(v.x, v.y); }
    }

    public struct Quaternion
    {
        public float x, y, z, w;
        public static Quaternion identity { get { return new Quaternion(); } }
        public static Quaternion Euler(float x, float y, float z) { return new Quaternion(); }
        public static Quaternion Euler(Vector3 v) { return new Quaternion(); }
    }

    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b) { this.r = r; this.g = g; this.b = b; this.a = 1f; }
        public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white { get { return new Color(1, 1, 1); } }
        public static Color black { get { return new Color(0, 0, 0); } }
        public static Color gray { get { return new Color(0.5f, 0.5f, 0.5f); } }
        public static Color clear { get { return new Color(0, 0, 0, 0); } }
        public static Color operator *(Color c, float f) { return c; }
    }

    public static class Mathf
    {
        public const float PI = 3.14159265f;
        public static bool Approximately(float a, float b) { return false; }
        public static float Sin(float f) { return 0f; }
        public static float Cos(float f) { return 0f; }
        public static float Abs(float f) { return f < 0 ? -f : f; }
        public static float Max(float a, float b) { return a > b ? a : b; }
        public static float Min(float a, float b) { return a < b ? a : b; }
        public static int Clamp(int value, int min, int max) { return value < min ? min : (value > max ? max : value); }
        public static float Clamp(float value, float min, float max) { return value < min ? min : (value > max ? max : value); }
        public static float Lerp(float a, float b, float t) { return a; }
    }

    public enum Space { Self, World }
    public enum PrimitiveType { Sphere, Capsule, Cylinder, Cube, Plane, Quad }
    public enum LightType { Spot, Directional, Point, Area, Disc }
    public enum LightShadows { None, Hard, Soft }
    public enum CameraClearFlags { Skybox = 1, Color = 2, Depth = 3, Nothing = 4 }
    public enum TextAnchor { UpperLeft, UpperCenter, UpperRight, MiddleLeft, MiddleCenter, MiddleRight, LowerLeft, LowerCenter, LowerRight }
    public enum TextAlignment { Left, Center, Right }
    public enum HorizontalWrapMode { Wrap, Overflow }
    public enum VerticalWrapMode { Truncate, Overflow }
    public enum RenderMode { ScreenSpaceOverlay, ScreenSpaceCamera, WorldSpace }
    public enum QueryTriggerInteraction { UseGlobal, Ignore, Collide }
    public enum KeyCode
    {
        None = 0, Backspace, Delete, Tab, Return, Escape, Space,
        A = 97, B, C, D, E, F, G, H, I, J, K, L, M,
        N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
        Alpha0, Alpha1, UpArrow, DownArrow, LeftArrow, RightArrow,
        LeftShift, RightShift, LeftControl, RightControl, F1
    }

    public struct LayerMask
    {
        public int value;
        public static implicit operator LayerMask(int intVal) { return new LayerMask { value = intVal }; }
        public static implicit operator int(LayerMask mask) { return mask.value; }
    }

    public struct Ray
    {
        public Vector3 origin;
        public Vector3 direction;
    }

    public struct RaycastHit
    {
        public Vector3 point;
        public Vector3 normal;
        public float distance;
        public Collider collider;
    }

    public static class Screen
    {
        public static int width { get { return 1280; } }
        public static int height { get { return 720; } }
    }

    public class Camera : Behaviour
    {
        public static Camera main { get { return null; } }
        public CameraClearFlags clearFlags { get; set; }
        public Color backgroundColor { get; set; }
        public float fieldOfView { get; set; }
        public float nearClipPlane { get; set; }
        public float farClipPlane { get; set; }
        public Ray ScreenPointToRay(Vector3 pos) { return new Ray(); }
    }

    public class AudioListener : Behaviour { }

    public class Light : Behaviour
    {
        public LightType type { get; set; }
        public Color color { get; set; }
        public float intensity { get; set; }
        public LightShadows shadows { get; set; }
    }

    public class Renderer : Component
    {
        public Material material { get; set; }
        public Material sharedMaterial { get; set; }
        public bool enabled { get; set; }
    }

    public class Material : Object
    {
        public Material(Shader shader) { }
        public Material(Material source) { }
        public bool HasProperty(string propertyName) { return false; }
        public void SetColor(string name, Color value) { }
        public void SetFloat(string name, float value) { }
        public Color color { get; set; }
        public Shader shader { get; set; }
    }

    public class Shader : Object
    {
        public static Shader Find(string name) { return null; }
    }

    public class Collider : Component { }
    public class BoxCollider : Collider { public Vector3 size { get; set; } public Vector3 center { get; set; } }
    public class SphereCollider : Collider { public Vector3 center { get; set; } public float radius { get; set; } }

    public class CharacterController : Collider
    {
        public float height { get; set; }
        public float radius { get; set; }
        public Vector3 center { get; set; }
        public float slopeLimit { get; set; }
        public float stepOffset { get; set; }
        public bool isGrounded { get { return true; } }
        public void Move(Vector3 motion) { }
    }

    public class Font : Object
    {
        public static Font CreateDynamicFontFromOSFont(string fontname, int size) { return null; }
        public static Font CreateDynamicFontFromOSFont(string[] fontnames, int size) { return null; }
        public bool HasCharacter(char c) { return false; }
    }

    public class TextMesh : Component
    {
        public string text { get; set; }
        public Font font { get; set; }
        public int fontSize { get; set; }
        public float characterSize { get; set; }
        public TextAnchor anchor { get; set; }
        public TextAlignment alignment { get; set; }
        public Color color { get; set; }
    }

    public class Canvas : Behaviour
    {
        public RenderMode renderMode { get; set; }
        public Camera worldCamera { get; set; }
    }

    public class CanvasScaler : Behaviour
    {
        public enum ScaleMode { ConstantPixelSize, ScaleWithScreenSize, ConstantPhysicalSize }
        public ScaleMode uiScaleMode { get; set; }
        public Vector2 referenceResolution { get; set; }
        public float matchWidthOrHeight { get; set; }
    }

    public class GraphicRaycaster : Behaviour { }

    public static class RenderSettings
    {
        public static Rendering.AmbientMode ambientMode { get; set; }
        public static Color ambientSkyColor { get; set; }
        public static Color ambientEquatorColor { get; set; }
        public static Color ambientGroundColor { get; set; }
        public static Color ambientLight { get; set; }
    }

    public static class Resources
    {
        public static T Load<T>(string path) where T : Object { return null; }
        public static T GetBuiltinResource<T>(string path) where T : Object { return null; }
    }

    public static class Debug
    {
        public static void Log(object message) { }
        public static void LogWarning(object message) { }
        public static void LogError(object message) { }
        public static void LogException(Exception e) { }
    }

    public static class Time
    {
        public static float time { get { return 0f; } }
        public static float deltaTime { get { return 0f; } }
    }

    public static class Input
    {
        public static Vector3 mousePosition { get { return new Vector3(); } }
        public static bool GetKey(KeyCode key) { return false; }
        public static bool GetKeyDown(KeyCode key) { return false; }
        public static bool GetMouseButton(int button) { return false; }
        public static bool GetMouseButtonDown(int button) { return false; }
        public static bool GetMouseButtonUp(int button) { return false; }
    }

    public static class Physics
    {
        public static bool Raycast(Ray ray, out RaycastHit hitInfo, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
        { hitInfo = new RaycastHit(); return false; }
        public static bool Raycast(Ray ray, out RaycastHit hitInfo, float maxDistance, LayerMask layerMask, QueryTriggerInteraction queryTriggerInteraction)
        { hitInfo = new RaycastHit(); return false; }
    }

    public static class JsonUtility
    {
        public static string ToJson(object obj) { return "{}"; }
        public static string ToJson(object obj, bool prettyPrint) { return "{}"; }
        public static T FromJson<T>(string json) { return default(T); }
    }

    public static class Application
    {
        public static string unityVersion { get { return ""; } }
        public static bool isPlaying { get { return false; } }
        public static bool isBatchMode { get { return false; } }
        public static bool isEditor { get { return true; } }
        public static string dataPath { get { return ""; } }
    }

    public static class GameObjectUtility
    {
        public static GameObject GetMainGameObject() { return null; }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class HeaderAttribute : Attribute { public HeaderAttribute(string header) { } }

    [AttributeUsage(AttributeTargets.Field)]
    public class TooltipAttribute : Attribute { public TooltipAttribute(string tooltip) { } }

    [AttributeUsage(AttributeTargets.Field)]
    public class SerializeField : Attribute { }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class RequireComponent : Attribute { public RequireComponent(Type requiredComponent) { } }
}

namespace UnityEngine.Rendering
{
    public enum AmbientMode { Skybox = 0, Trilight = 1, Flat = 3, Custom = 4 }
}

namespace UnityEngine.SceneManagement
{
    using System.Collections.Generic;
    using UnityEngine;

    public struct Scene
    {
        public string name { get { return ""; } }
        public bool IsValid() { return true; }
        public bool isLoaded { get { return true; } }
        public GameObject[] GetRootGameObjects() { return new GameObject[0]; }
        public int GetRootGameObjectCount() { return 0; }
    }

    public static class SceneManager
    {
        public static Scene GetActiveScene() { return new Scene(); }
        public static int sceneCount { get { return 1; } }
    }
}

namespace UnityEditor
{
    using System;
    using UnityEngine;

    [AttributeUsage(AttributeTargets.Method)]
    public class MenuItem : Attribute
    {
        public MenuItem(string itemName) { }
        public MenuItem(string itemName, bool isValidateFunction) { }
        public MenuItem(string itemName, bool isValidateFunction, int priority) { }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class InitializeOnLoadAttribute : Attribute { }

    public static class SessionState
    {
        public static bool GetBool(string key, bool defaultValue) { return defaultValue; }
        public static void SetBool(string key, bool value) { }
    }

    public static class EditorUtility
    {
        public static bool DisplayDialog(string title, string message, string ok) { return true; }
        public static bool DisplayDialog(string title, string message, string ok, string cancel) { return true; }
        public static void DisplayProgressBar(string title, string info, float progress) { }
        public static void ClearProgressBar() { }
    }

    public static class AssetDatabase
    {
        public static void Refresh() { }
        public static void SaveAssets() { }
    }

    public static class EditorApplication
    {
        public static bool isPlaying { get { return false; } }
        public static bool isPlayingOrWillChangePlaymode { get { return false; } }
        public static double timeSinceStartup { get { return 0d; } }
        public static Action update;
        public static void EnterPlaymode() { }
        public static void Exit(int returnValue) { }
    }

    public static class Selection
    {
        public static GameObject activeGameObject { get; set; }
        public static GameObject[] objects { get; set; }
    }
}

namespace UnityEditor.SceneManagement
{
    using UnityEngine.SceneManagement;

    public enum NewSceneSetup { EmptyScene, DefaultGameObjects }
    public enum NewSceneMode { Single, Additive }
    public enum OpenSceneMode { Single, Additive, SingleCommitted }

    public static class EditorSceneManager
    {
        public static Scene NewScene(NewSceneSetup setup, NewSceneMode mode) { return new Scene(); }
        public static Scene NewScene(NewSceneSetup setup) { return new Scene(); }
        public static bool SaveScene(Scene scene) { return true; }
        public static bool SaveScene(Scene scene, string destinationPath) { return true; }
        public static Scene OpenScene(string path) { return new Scene(); }
        public static Scene OpenScene(string path, OpenSceneMode mode) { return new Scene(); }
    }
}

namespace UnityEngine.Events
{
    public delegate void UnityAction();
    public delegate void UnityAction<T0>(T0 arg0);
}

namespace UnityEngine.EventSystems
{
    using UnityEngine;

    public class EventSystem : UIBehaviour
    {
        public static EventSystem current { get { return null; } }
        public GameObject currentSelectedGameObject { get { return null; } }
        public void SetSelectedGameObject(GameObject selected) { }
        public bool IsPointerOverGameObject() { return false; }
        public bool IsPointerOverGameObject(int pointerId) { return false; }
    }

    public class UIBehaviour : MonoBehaviour { }

    public class StandaloneInputModule : UIBehaviour { }
}

namespace UnityEngine.Networking
{
    using System;
    using UnityEngine;

    public class DownloadHandler : IDisposable { public string text { get { return ""; } } public void Dispose() { } }
    public class DownloadHandlerBuffer : DownloadHandler { }
    public class UploadHandler : IDisposable { public void Dispose() { } }
    public class UploadHandlerRaw : UploadHandler { public UploadHandlerRaw(byte[] data) { } }

    public class UnityWebRequestAsyncOperation : IEnumerator
    {
        public bool isDone { get { return true; } }
        public object Current { get { return null; } }
        public bool MoveNext() { return false; }
        public void Reset() { }
        public IEnumerator GetEnumerator() { yield break; }
    }

    public class UnityWebRequest : IDisposable
    {
        public enum Result { InProgress, Success, ConnectionError, ProtocolError, DataProcessingError }

        public const string kHttpVerbGET = "GET";
        public const string kHttpVerbPOST = "POST";
        public const string kHttpVerbPUT = "PUT";
        public const string kHttpVerbPATCH = "PATCH";
        public const string kHttpVerbDELETE = "DELETE";

        public UnityWebRequest() { }
        public UnityWebRequest(string url) { }
        public UnityWebRequest(string url, string method) { }
        public UploadHandler uploadHandler { get; set; }
        public DownloadHandler downloadHandler { get; set; }
        public int timeout { get; set; }
        public long responseCode { get { return 0; } }
        public string error { get { return ""; } }
        public Result result { get { return Result.Success; } }
        public bool isNetworkError { get { return false; } }
        public bool isHttpError { get { return false; } }
        public void SetRequestHeader(string name, string value) { }
        public string GetRequestHeader(string name) { return ""; }
        public UnityWebRequestAsyncOperation SendWebRequest() { return null; }
        public void Abort() { }
        public void Dispose() { }
        public static UnityWebRequest Get(string uri) { return null; }
        public static string EscapeURL(string s) { return s; }
        public static string UnEscapeURL(string s) { return s; }
    }
}

namespace UnityEngine.UI
{
    using UnityEngine;
    using UnityEngine.Events;

    public struct ColorBlock
    {
        public Color normalColor;
        public Color highlightedColor;
        public Color pressedColor;
        public Color selectedColor;
        public Color disabledColor;
        public float colorMultiplier;
        public float fadeDuration;
    }

    public class Graphic : Behaviour
    {
        public Color color { get; set; }
        public bool raycastTarget { get; set; }
        public RectTransform rectTransform { get { return null; } }
    }

    public class MaskableGraphic : Graphic { }

    public class Image : MaskableGraphic { }

    public class Text : MaskableGraphic
    {
        public Font font { get; set; }
        public int fontSize { get; set; }
        public string text { get; set; }
        public TextAnchor alignment { get; set; }
        public HorizontalWrapMode horizontalOverflow { get; set; }
        public VerticalWrapMode verticalOverflow { get; set; }
        public bool supportRichText { get; set; }
        public bool resizeTextForBestFit { get; set; }
    }

    public class Selectable : Behaviour
    {
        public bool interactable { get; set; }
        public Graphic targetGraphic { get; set; }
        public ColorBlock colors { get; set; }
        public void Select() { }
    }

    public class Button : Selectable
    {
        public class ButtonClickedEvent
        {
            public void AddListener(UnityAction call) { }
            public void RemoveListener(UnityAction call) { }
        }
        public ButtonClickedEvent onClick { get { return new ButtonClickedEvent(); } }
    }

    public class InputField : Selectable
    {
        public enum LineType { SingleLine, MultiLineSubmit, MultiLineNewline }
        public enum ContentType { Standard, Autocorrected, IntegerNumber, DecimalNumber, Alphanumeric, Name, EmailAddress, Password, Pin, Custom }

        public string text { get; set; }
        public Graphic placeholder { get; set; }
        public Graphic textComponent { get; set; }
        public LineType lineType { get; set; }
        public ContentType contentType { get; set; }
        public int characterLimit { get; set; }
        public Color caretColor { get; set; }
        public bool customCaretColor { get; set; }
        public int caretPosition { get; set; }
        public bool isFocused { get { return false; } }
        public void MoveTextEnd(bool shift) { }
    }

    public class Toggle : Selectable
    {
        public class ToggleEvent
        {
            public void AddListener(UnityAction<bool> call) { }
        }
        public ToggleEvent onValueChanged { get { return new ToggleEvent(); } }
        public bool isOn { get; set; }
    }
}
