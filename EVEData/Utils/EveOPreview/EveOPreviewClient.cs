using System.IO.Pipes;
using System.Text.Json;
using System.Diagnostics;

namespace EVEData.Utils.EveOPreview
{
    public static class EveOPreviewClient
    {
        private static string _pipename = "EVE-O-Preview-Pipe";
        private static void SendCommand(object command)
        {
            try
            {
                using (var pipe = new NamedPipeClientStream(".", _pipename, PipeDirection.InOut))
                {
                    pipe.Connect(500);

                    using (var reader = new StreamReader(pipe))
                    using (var writer = new StreamWriter(pipe)
                    {
                        AutoFlush = true
                    })
                    {
                        writer.WriteLineAsync(JsonSerializer.Serialize(command));
                        reader.ReadLineAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error sending command to EveOPreview: {ex.Message}");
            }
        }

        public static void UpdateSystem(string clientName, string systemName)
        {
            Debug.WriteLine($"EveOPreview UpdateSystem: {clientName} {systemName}");

            var systemCommand = new EveOEnvelope(
                Guid.NewGuid(),
                EveOSystemUpdate.MessageType,
                JsonSerializer.SerializeToElement(
                    new EveOSystemUpdate
                    {
                        Client = clientName,
                        SystemName = systemName
                    }));

            SendCommand(systemCommand);
        }
        public static void AlertClient(EveOAlertClient alert)
        {
            /*

            if (alert.isFaction)
            {
                eveoAlert.AlertType = EveOAlertClient.AlertTypeFaction;
                eveoAlert.Client = alert.Entry.CharacterName;
                eveoAlert.AlertText = "Faction Spawn";
            }

            if (alert.isMiningOver)
            {
                eveoAlert.AlertType = EveOAlertClient.AlertTypeMiningOver;
                eveoAlert.AlertText = "Mining over";
                eveoAlert.Client = alert.Entry.CharacterName;
            }

            if (alert.Kill != null)
            {
                eveoAlert.AlertType = EveOAlertClient.AlertTypeKill;
                eveoAlert.AlertText = alert.Kill.ID.ToString();
            }
            */

            var alertCommand = new EveOEnvelope(
                Guid.NewGuid(),
                EveOAlertClient.MessageType,
                JsonSerializer.SerializeToElement(alert));

            SendCommand(alertCommand);
        }

        public static void Agression(string clientName, bool agression)
        {
            var agressionCommand = new EveOEnvelope(
                Guid.NewGuid(),
                EveOAgression.MessageType,
                JsonSerializer.SerializeToElement(
                    new EveOAgression
                    {
                        Client = clientName,
                        Agression = agression
                    }));

            SendCommand(agressionCommand);
        }
    }
}