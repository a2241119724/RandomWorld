# -*- coding: utf-8 -*-
"""无人值守单测闭环：编主程序集 ref → 编 Editor 测试程序集 → Mono 跑极简 NUnit runner。

Unity 定制 NUnit 是 net35（内部用 CallContext，.NET 6 已删 Remoting）→ 必须用
Unity 自带 Mono 运行时跑。runner 源码内嵌本脚本（放 Assets 内会被 Unity 当游戏
代码编译，故运行时写出到工程根 _verify_out）。输出严禁落 Assets 内（CS0433 教训）。

用法（cwd 任意）：python Assets/tmp/verify_tests.py
依赖：Assembly-CSharp.csproj / Assembly-CSharp-Editor.csproj（Unity 生成，refs 来源）
退出码 = 测试失败数>0 ? 1 : 0；编译失败退出码 2。
"""
import re
import subprocess
import sys
from pathlib import Path

ASSETS = Path(__file__).resolve().parent.parent      # Assets/
PROJ = ASSETS.parent                                 # 工程根（git 仓库是 Assets/，输出在仓库外）
OUT = PROJ / "_verify_out"
UNITY = Path(r"D:/APP/Unity/Unity/2022.3.62f2c1/Editor/Data")
CSC = UNITY / "DotNetSdkRoslyn" / "csc.dll"
DOTNET = UNITY / "NetCoreRuntime" / "dotnet.exe"
MONO = UNITY / "MonoBleedingEdge" / "bin" / "mono.exe"
MONO_BCL = UNITY / "MonoBleedingEdge" / "lib" / "mono" / "4.7.1-api"
NUNIT = PROJ / "Library" / "PackageCache" / "com.unity.ext.nunit@1.0.6" / "net35" / "unity-custom" / "nunit.framework.dll"

# 与 verify_compile.py 相同的主程序集源排除规则（csproj 严重过期，磁盘全扫）
MAIN_EXCLUDE_PREFIX = ("Scripts/Reference/Photon/", "Scripts/2D/Editor/", "tmp/",
                       "Library/", "TextMesh Pro/Editor/")
MAIN_EXCLUDE_IN = ("/Editor/", "/Tests/")
# 已知编译不过的文件（RoundCorner.cs：dc5a5552「圆角合批」重写从未过 Unity 编译——
# IsActive 无括号（UIBehaviour 是方法）+ AddVert 5 参重载不存在。待并行会话修复后移除，
# 期间用下方 STUB 替代保住 TipUI 等引用方编译）
MAIN_EXCLUDE_FILES = {"Scripts/2D/RoundCorner.cs"}

# RoundCorner 编译 stub（TipUI 只用 Graphic.color 成员，空壳足够）
ROUND_CORNER_STUB_CS = """// verify_tests.py 生成：RoundCorner.cs 编译不过时的临时替代（勿手动编辑）
namespace LAB2D
{
    public class RoundCorner : UnityEngine.UI.MaskableGraphic
    {
        public float Radius = 0.5f;
    }
}
"""


def dec(b):
    for enc in ("utf-8", "gbk", "latin-1"):
        try:
            return b.decode(enc)
        except UnicodeDecodeError:
            continue
    return ""


def run_csc(rsp_path, log_name):
    r = subprocess.run([str(DOTNET), "exec", str(CSC), "@%s" % rsp_path],
                       capture_output=True, cwd=str(PROJ), timeout=600)
    log = dec(r.stdout or b"") + dec(r.stderr or b"")
    (OUT / log_name).write_text(log, encoding="utf-8")
    errs = [l for l in log.splitlines() if "error CS" in l]
    for l in errs[:20]:
        print(l)
    if errs:
        print("== %s 编译失败（%d 错误，详见 _verify_out/%s）==" % (rsp_path.name, len(errs), log_name))
        sys.exit(2)
    return True


def csproj_refs_and_defines(csproj_path):
    text = csproj_path.read_text(encoding="utf-8")
    refs = re.findall(r"<HintPath>([^<]+)</HintPath>", text)
    m = re.search(r"<DefineConstants>([^<]*)</DefineConstants>", text)
    defines = m.group(1).split(";") if m else []
    return [str(PROJ / r) for r in refs], [d for d in defines if d]


def scan_sources(root_rel, exclude_prefix, exclude_in):
    out = []
    for p in (ASSETS / root_rel).rglob("*.cs"):
        rel = p.relative_to(ASSETS).as_posix()
        if rel.startswith(exclude_prefix) or any(s in rel for s in exclude_in):
            continue
        if rel in MAIN_EXCLUDE_FILES:
            stub = OUT / "RoundCorner_Stub.cs"
            stub.write_text(ROUND_CORNER_STUB_CS, encoding="utf-8")
            out.append(str(stub))  # 用 stub 顶替，保住引用方（TipUI.color）编译
            continue
        out.append("Assets/" + rel)  # rsp 以工程根为 cwd，须带 Assets/ 前缀
    return sorted(out)


def build_main():
    """主程序集 → verify_Assembly-CSharp.dll + .ref.dll（Editor 测试程序集的引用底座）。"""
    refs, defines = csproj_refs_and_defines(PROJ / "Assembly-CSharp.csproj")
    refs += [str(p) for p in (PROJ / "Library" / "ScriptAssemblies").glob("Photon*.dll")
             if "Editor" not in p.name]
    sources = scan_sources("Scripts", MAIN_EXCLUDE_PREFIX, MAIN_EXCLUDE_IN)
    rsp = OUT / "verify.rsp"
    with rsp.open("w", encoding="utf-8") as f:
        f.write("-target:library -nologo -nowarn:CS0169;CS0649\n")
        for d in defines:
            f.write("-define:%s\n" % d)
        f.write('-out:"_verify_out/verify_Assembly-CSharp.dll"\n')
        f.write('-refout:"_verify_out/verify_Assembly-CSharp.ref.dll"\n')
        for r in refs:
            f.write('-r:"%s"\n' % r)
        for s in sources:
            f.write('"%s"\n' % s)
    print("主程序集 sources=%d refs=%d" % (len(sources), len(refs)))
    run_csc(rsp, "verify_main.log")


def build_editor_tests():
    """Editor 测试程序集 → verify_Editor.dll（Scripts/2D/Editor 磁盘全扫 + 主 ref.dll）。"""
    refs, defines = csproj_refs_and_defines(PROJ / "Assembly-CSharp-Editor.csproj")
    refs.append(str(OUT / "verify_Assembly-CSharp.ref.dll"))
    # 主程序集的 AWorker 继承 MonoBehaviourPun → Editor 侧类型解析需传递引用 Photon
    refs += [str(p) for p in (PROJ / "Library" / "ScriptAssemblies").glob("Photon*.dll")
             if "Editor" not in p.name]
    sources = scan_sources("Scripts/2D/Editor", (), ())
    rsp = OUT / "verify_editor.rsp"
    with rsp.open("w", encoding="utf-8") as f:
        f.write("-target:library -nologo -nowarn:CS0169;CS0649\n")
        for d in defines:
            f.write("-define:%s\n" % d)
        f.write('-out:"_verify_out/verify_Editor.dll"\n')
        f.write('-refout:"_verify_out/verify_Editor.ref.dll"\n')
        for r in refs:
            f.write('-r:"%s"\n' % r)
        for s in sources:
            f.write('"%s"\n' % s)
    print("测试程序集 sources=%d refs=%d" % (len(sources), len(refs)))
    run_csc(rsp, "verify_editor.log")


TEST_RUNNER_CS = r'''// 极简 NUnit 反射 runner（无人值守单测闭环，2026-09-04；由 Assets/tmp/verify_tests.py 写出）
// 跑 Unity Test Framework 之外的 Domain 纯函数测试：verify_Editor.dll 里的
// [TestFixture]+[Test]（SetUp/OneTimeSetUp 支持），逐 fixture 容错（类型解析失败记 skip 不炸全局）。
// 必须用 Unity 自带 Mono 运行（net35 NUnit 内部 CallContext，.NET 6 无 Remoting）。
using System;
using System.IO;
using System.Linq;
using System.Reflection;

internal static class TestRunner
{
    private static int total, passed, failed, skipped;

    private static int Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        if (args.Length < 1 || !File.Exists(args[0]))
        {
            Console.WriteLine("usage: test_runner <test-dll> [fixture-filter]");
            return 2;
        }

        // 可选第二参数：只跑名字包含该子串的 fixture（跨测试静态污染二分排查用）
        string filter = args.Length > 1 ? args[1] : null;

        AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;

        Assembly asm;
        try
        {
            asm = Assembly.LoadFrom(args[0]);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FATAL: 加载测试程序集失败: {ex.Message}");
            return 2;
        }

        Type[] types;
        try
        {
            types = asm.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = ex.Types.Where(t => t != null).ToArray();
        }

        foreach (Type fixture in types)
        {
            if (filter != null && fixture.Name.IndexOf(filter, StringComparison.Ordinal) < 0)
            {
                continue;
            }

            RunFixture(fixture);
        }

        Console.WriteLine($"\n==== 总计 {total} | 通过 {passed} | 失败 {failed} | 跳过 {skipped} ====");
        return failed > 0 ? 1 : 0;
    }

    private static void RunFixture(Type fixture)
    {
        MethodInfo[] tests;
        try
        {
            tests = fixture.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Where(m => m.GetCustomAttributes(typeof(NUnit.Framework.TestAttribute), false).Length > 0)
                .ToArray();
        }
        catch (Exception ex)
        {
            skipped++;
            Console.WriteLine($"SKIP  {fixture.FullName}: 方法扫描失败 {ex.Message}");
            return;
        }

        if (tests.Length == 0)
        {
            return;
        }

        MethodInfo setup = FindSetup(fixture, "SetUpAttribute");
        MethodInfo oneTimeSetup = FindSetup(fixture, "OneTimeSetUpAttribute");
        MethodInfo tearDown = FindSetup(fixture, "TearDownAttribute");

        object oneTimeInstance = null;
        foreach (MethodInfo test in tests)
        {
            total++;
            string label = $"{fixture.Name}.{test.Name}";
            object instance = null;
            try
            {
                instance = test.IsStatic ? null : Activator.CreateInstance(fixture);
                if (instance != null)
                {
                    setup?.Invoke(instance, null);
                }

                if (oneTimeSetup != null && oneTimeInstance == null)
                {
                    if (oneTimeSetup.IsStatic)
                    {
                        oneTimeSetup.Invoke(null, null);
                        oneTimeInstance = new object();
                    }
                    else
                    {
                        oneTimeInstance = instance ?? Activator.CreateInstance(fixture);
                        oneTimeSetup.Invoke(oneTimeInstance, null);
                    }
                }

                test.Invoke(test.IsStatic ? null : instance, null);
                passed++;
            }
            catch (Exception ex)
            {
                failed++;
                Exception root = ex is TargetInvocationException && ex.InnerException != null ? ex.InnerException : ex;
                Console.WriteLine($"FAIL  {label}: {root.Message}");
                Console.WriteLine($"      {root.StackTrace?.Split('\n').FirstOrDefault(s => s.Contains("Tests")) ?? "(无测试栈帧)"}");
            }
            finally
            {
                // TearDown 必须无条件跑——测试间静态桩清理（如 RandomFloatProvider 置 null）
                // 缺了它残留桩会让后续测试必 miss（TurnBattle 6 连败的根因）
                try
                {
                    if (tearDown != null && !tearDown.IsStatic)
                    {
                        object teardownInstance = instance ?? Activator.CreateInstance(fixture);
                        tearDown.Invoke(teardownInstance, null);
                    }
                    else if (tearDown != null)
                    {
                        tearDown.Invoke(null, null);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"WARN  {label}: TearDown 抛异常 {ex.Message}");
                }
            }
        }
    }

    private static MethodInfo FindSetup(Type fixture, string attrName)
    {
        try
        {
            foreach (MethodInfo m in fixture.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
            {
                foreach (Attribute a in m.GetCustomAttributes(false))
                {
                    if (a.GetType().Name == attrName)
                    {
                        return m;
                    }
                }
            }
        }
        catch
        {
            // 容错：扫描失败当无 SetUp
        }

        return null;
    }

    private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
    {
        string name = new AssemblyName(args.Name).Name;
        string baseDir = AppContext.BaseDirectory;

        // nunit.framework（强名 3.5.0.0）→ PackageCache 的 Unity 定制版
        if (name == "nunit.framework")
        {
            string nunit = Path.Combine(baseDir, "..", "Library", "PackageCache",
                "com.unity.ext.nunit@1.0.6", "net35", "unity-custom", "nunit.framework.dll");
            return File.Exists(nunit) ? Assembly.LoadFrom(nunit) : null;
        }

        // UnityEngine.* → Unity Editor Managed 目录（DayNightRuleServiceTests 需要）
        if (name.StartsWith("UnityEngine", StringComparison.Ordinal))
        {
            string unityManaged = Path.Combine(
                @"D:\APP\Unity\Unity\2022.3.62f2c1\Editor\Data\Managed\UnityEngine", name + ".dll");
            return File.Exists(unityManaged) ? Assembly.LoadFrom(unityManaged) : null;
        }

        // 其余（Photon 等）→ Library/ScriptAssemblies
        string scriptAsm = Path.Combine(baseDir, "..", "Library", "ScriptAssemblies", name + ".dll");
        return File.Exists(scriptAsm) ? Assembly.LoadFrom(scriptAsm) : null;
    }
}
'''


def build_runner():
    """runner → test_runner.exe（Mono 4.7.1-api BCL，供 mono.exe 执行）。"""
    cs = OUT / "TestRunner.cs"
    cs.write_text(TEST_RUNNER_CS, encoding="utf-8")
    rsp = OUT / "runner.rsp"
    with rsp.open("w", encoding="utf-8") as f:
        f.write("-nologo -out:_verify_out/test_runner.exe\n")
        for lib in ("mscorlib.dll", "System.dll", "System.Core.dll"):
            f.write('-r:"%s"\n' % (MONO_BCL / lib))
        f.write('-r:"%s"\n' % NUNIT)
        f.write('"%s"\n' % cs)
    run_csc(rsp, "verify_runner.log")


def run_tests():
    r = subprocess.run([str(MONO), "test_runner.exe", "verify_Editor.dll"],
                       capture_output=True, cwd=str(OUT), timeout=600)
    log = dec(r.stdout or b"") + dec(r.stderr or b"")
    (OUT / "test_results.txt").write_text(log, encoding="utf-8")
    fails = [l for l in log.splitlines() if l.startswith("FAIL")]
    for l in fails[:40]:
        print(l)
    if len(fails) > 40:
        print("...（其余 %d 条 FAIL 见 _verify_out/test_results.txt）" % (len(fails) - 40))
    stat = [l for l in log.splitlines() if l.startswith("==== 总计")]
    print(stat[0] if stat else "（无统计行，疑似 FATAL——看 _verify_out/test_results.txt）")
    sys.exit(1 if r.returncode != 0 else 0)


if __name__ == "__main__":
    OUT.mkdir(exist_ok=True)
    for step in (build_main, build_editor_tests, build_runner, run_tests):
        step()
