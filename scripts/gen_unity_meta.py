#!/usr/bin/env python3
"""为 Unity 工程的 Assets 目录生成 .meta 文件。

GUID 用「相对路径的 md5」生成，保证：
  1. 每次运行结果一致，重复执行不会改已有 .meta；
  2. 不同文件之间不会撞 GUID。
已有 .meta 的文件不会被覆盖。
"""
import hashlib
import os
import sys

ROOT = os.path.abspath(sys.argv[1] if len(sys.argv) > 1 else "unity/InspectionDemo")
ASSETS = os.path.join(ROOT, "Assets")


def guid_for(path: str) -> str:
    rel = os.path.relpath(path, ROOT).replace(os.sep, "/")
    return hashlib.md5(("inspection-demo::" + rel).encode("utf-8")).hexdigest()


FOLDER_META = """fileFormatVersion: 2
guid: {guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
"""

MONO_META = """fileFormatVersion: 2
guid: {guid}
MonoImporter:
  externalObjects: {{}}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {{instanceID: 0}}
  userData:
  assetBundleName:
  assetBundleVariant:
"""

DEFAULT_META = """fileFormatVersion: 2
guid: {guid}
DefaultImporter:
  externalObjects: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
"""

NATIVE_META = """fileFormatVersion: 2
guid: {guid}
NativeFormatImporter:
  externalObjects: {{}}
  mainObjectFileID: {file_id}
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def meta_body(path: str) -> str:
    if os.path.isdir(path):
        return FOLDER_META.format(guid=guid_for(path))
    ext = os.path.splitext(path)[1].lower()
    if ext == ".cs":
        return MONO_META.format(guid=guid_for(path))
    if ext in (".mat", ".asset"):
        return NATIVE_META.format(guid=guid_for(path), file_id=2100000)
    return DEFAULT_META.format(guid=guid_for(path))


def main() -> None:
    created = 0
    for dirpath, dirnames, filenames in os.walk(ASSETS):
        dirnames.sort()
        # 目录本身的 .meta
        for d in dirnames:
            folder = os.path.join(dirpath, d)
            meta = folder + ".meta"
            if not os.path.exists(meta):
                with open(meta, "w", encoding="utf-8") as f:
                    f.write(meta_body(folder))
                created += 1
        for name in sorted(filenames):
            if name.endswith(".meta"):
                continue
            target = os.path.join(dirpath, name)
            meta = target + ".meta"
            if not os.path.exists(meta):
                with open(meta, "w", encoding="utf-8") as f:
                    f.write(meta_body(target))
                created += 1

    print(f"新建 .meta 文件: {created} 个")
    for rel in ("Assets/Scenes/Main.unity", "Assets/Scripts/Core/SceneBootstrap.cs",
                "Assets/Resources/InspectionDefault.mat"):
        p = os.path.join(ROOT, rel)
        if os.path.exists(p + ".meta"):
            print(f"{rel}\t{guid_for(p)}")


if __name__ == "__main__":
    main()
