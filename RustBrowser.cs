using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

// Лаунчер Rust Server Browser.
// HTML зашит внутрь exe как ресурс. При запуске распаковывается во временную
// папку и открывается в Microsoft Edge в режиме "приложения" (чистое окно
// без вкладок и адресной строки). Если Edge не найден — открывается в браузере
// по умолчанию. Один переносимый exe, без зависимостей.
class RustBrowser
{
    [STAThread]
    static void Main()
    {
        try
        {
            string dir = Path.Combine(Path.GetTempPath(), "RustServerBrowser");
            Directory.CreateDirectory(dir);
            string htmlPath = Path.Combine(dir, "index.html");

            // 1) Достаём встроенный index.html из ресурсов exe
            Assembly asm = Assembly.GetExecutingAssembly();
            bool extracted = false;
            using (Stream s = asm.GetManifestResourceStream("index.html"))
            {
                if (s != null)
                {
                    using (FileStream fs = File.Create(htmlPath))
                        s.CopyTo(fs);
                    extracted = true;
                }
            }

            // 2) Фолбэк: index.html рядом с exe
            if (!extracted || !File.Exists(htmlPath))
            {
                string local = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "index.html");
                if (File.Exists(local)) htmlPath = local;
                else throw new FileNotFoundException("index.html не найден ни внутри exe, ни рядом с ним.");
            }

            string url = "file:///" + htmlPath.Replace("\\", "/");

            // 3) Запуск в Edge (app mode) с изолированным профилем
            string edge = FindEdge();
            if (edge != null)
            {
                string profile = Path.Combine(dir, "edge-profile");
                var psi = new ProcessStartInfo
                {
                    FileName = edge,
                    Arguments = "--app=\"" + url + "\""
                              + " --user-data-dir=\"" + profile + "\""
                              + " --window-size=1340,900"
                              + " --no-first-run --no-default-browser-check",
                    UseShellExecute = false
                };
                Process.Start(psi);
            }
            else
            {
                // Фолбэк: браузер по умолчанию
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Не удалось запустить Rust Server Browser:\n\n" + ex.Message,
                "Rust Server Browser", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    static string FindEdge()
    {
        string[] paths =
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Microsoft\Edge\Application\msedge.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),    @"Microsoft\Edge\Application\msedge.exe")
        };
        foreach (string p in paths)
            if (File.Exists(p)) return p;
        return null;
    }
}
