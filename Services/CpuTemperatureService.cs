namespace GelitaITToolkit.Services
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using LibreHardwareMonitor.Hardware;

    internal sealed record CpuTemperatureReading(double? Celsius, string Detail, string Message);

    internal static class CpuTemperatureService
    {
        public static async Task<CpuTemperatureReading> ReadAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var direct = await Task.Run(() => ReadDirect(cancellationToken), cancellationToken).ConfigureAwait(false);
            if (direct.Celsius.HasValue) return direct;
            cancellationToken.ThrowIfCancellationRequested();
            var published = await ReadPublishedAsync(cancellationToken).ConfigureAwait(false);
            return published.HasValue
                ? new CpuTemperatureReading(published, "Sensor publicado por monitor externo", "Temperatura atualizada a cada 15 s · Maior leitura dos sensores da CPU")
                : direct;
        }

        private static CpuTemperatureReading ReadDirect(CancellationToken cancellationToken)
        {
            var unavailable = new CpuTemperatureReading(null, "Sensor não disponível",
                "O sensor da CPU não está acessível. Verifique a compatibilidade do hardware e as permissões do Windows.");
            // The optional installer is a separate, explicitly user-triggered operation.
            if (!PawnIoInstallationService.IsInstalled)
                return new CpuTemperatureReading(null, "Driver de sensor ausente",
                    "Para habilitar a temperatura, use Instalar PawnIO. A instalação do driver é opcional.");
            var computer = new Computer { IsCpuEnabled = true };
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                computer.Open();
                double? maximum = null;
                foreach (var hardware in computer.Hardware.Where(h => h.HardwareType == HardwareType.Cpu))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    hardware.Update();
                    foreach (var sensor in hardware.Sensors.Where(s => s.SensorType == SensorType.Temperature))
                    {
                        if (sensor.Value is not float value || !float.IsFinite(value) || value <= 0 || value >= 125) continue;
                        maximum = maximum.HasValue ? Math.Max(maximum.Value, value) : value;
                    }
                }
                return maximum.HasValue
                    ? new CpuTemperatureReading(maximum, "Leitura direta · Maior sensor da CPU", string.Empty)
                    : unavailable;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or System.ComponentModel.Win32Exception or System.Security.SecurityException)
            {
                return unavailable;
            }
            finally { computer.Close(); }
        }

        // ACPI zones may describe other components; they are never labeled as CPU sensors.
        private static async Task<double?> ReadPublishedAsync(CancellationToken cancellationToken)
        {
            const string script = "foreach($ns in @('root/LibreHardwareMonitor','root/OpenHardwareMonitor')) { " +
                "try { $cpus=@(Get-CimInstance -Namespace $ns -ClassName Hardware -Filter \"HardwareType='CPU'\" -ErrorAction Stop | Select-Object -ExpandProperty Identifier); " +
                "$s=@(Get-CimInstance -Namespace $ns -ClassName Sensor -Filter \"SensorType='Temperature'\" -ErrorAction Stop | " +
                "Where-Object { $_.Parent -in $cpus -and $null -ne $_.Value -and $_.Value -gt 0 -and $_.Value -lt 125 }); " +
                "if($s.Count -gt 0) { $v=($s | Measure-Object -Property Value -Maximum).Maximum; " +
                "$v.ToString([Globalization.CultureInfo]::InvariantCulture); break } } catch {} }";
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(4));
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe"),
                    Arguments = "-NoLogo -NoProfile -NonInteractive -EncodedCommand " + Convert.ToBase64String(Encoding.Unicode.GetBytes(script)),
                    UseShellExecute = false, CreateNoWindow = true,
                    RedirectStandardOutput = true, RedirectStandardError = true
                }
            };
            try
            {
                process.Start();
                var output = process.StandardOutput.ReadToEndAsync(timeout.Token);
                var errors = process.StandardError.ReadToEndAsync(timeout.Token);
                await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
                var value = (await output.ConfigureAwait(false)).Trim();
                await errors.ConfigureAwait(false);
                return process.ExitCode == 0 && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var temperature)
                    && double.IsFinite(temperature) && temperature > 0 && temperature < 125 ? temperature : null;
            }
            catch (OperationCanceledException) { return null; }
            catch (IOException) { return null; }
            catch (System.ComponentModel.Win32Exception) { return null; }
            finally
            {
                try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
                catch (InvalidOperationException) { }
                catch (System.ComponentModel.Win32Exception) { }
            }
        }
    }
}
