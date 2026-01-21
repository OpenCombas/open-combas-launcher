using CombasLauncherApp.Models;
using SoulsFormats;
using SoulsFormats.Other.KF4;
using System.IO;

namespace CombasLauncherApp.Services.Implementations
{
    /// <summary>
    /// Service for reading and writing map configuration from MenuMapInfo.bin and MenuText_Eng.fmg files.
    /// </summary>
    public class MapConfigurationService
    {
        /// <summary>
        /// Reads map configuration data from the specified binary and localization files, and returns a list of map
        /// entries with localized names.
        /// </summary>
        /// <param name="binFilePath">The path to the binary file containing map configuration data. The file must exist.</param>
        /// <param name="paramDefPath">The path to the param def xml file</param>
        /// <param name="fmgFilePath">The path to the localization file containing map names. The file must exist.</param>
        /// <returns>A list of <see cref="MapEntry"/> objects representing the maps defined in the configuration. The list will
        /// be empty if no maps are found.</returns>
        /// <exception cref="FileNotFoundException">Thrown if <paramref name="binFilePath"/> or <paramref name="fmgFilePath"/> does not exist.</exception>
        public List<MapEntry> ReadMapConfiguration(string binFilePath, string paramDefPath, string fmgFilePath)
        {
            if (!File.Exists(binFilePath))
            {
                throw new FileNotFoundException($"Binary file not found: {binFilePath}");
            }

            if (!File.Exists(fmgFilePath))
            {
                throw new FileNotFoundException($"Localization file not found: {fmgFilePath}");
            }

            var mapNames = ExtractMapNames(fmgFilePath);
            var maps = ReadMapEntries(binFilePath, paramDefPath, mapNames);

            return maps;
        }

        /// <summary>
        /// Extracts a list of map names from the specified FMG file.
        /// </summary>
        /// <remarks>This method relies on an external tool to convert the FMG file to XML before parsing.
        /// The FMG file must be accessible and compatible with the extraction process.</remarks>
        /// <param name="fmgFilePath">The path to the FMG file from which map names are to be extracted. Must refer to a valid FMG file on disk.</param>
        /// <returns>A list of strings containing the extracted map names. The list will be empty if no map names are found.</returns>
        /// <exception cref="Exception">Thrown if the external extraction process fails to convert the FMG file to XML.</exception>
        private Dictionary<int, string> ExtractMapNames(string fmgFilePath)
        {
            var fmg = FMG.Read(fmgFilePath);

            // In this title, map names appear to be in ID range 5100..5209
            var mapNames = new Dictionary<int, string>();
            foreach (var entry in fmg.Entries)
            {
                if (entry.ID is >= 5100 and <= 5209 && !string.IsNullOrEmpty(entry.Text))
                {
                    mapNames.Add(entry.ID ,entry.Text.Trim());
                }
            }

            return mapNames;
        }


        /// <summary>
        /// Reads map entries from a binary file and returns a list of parsed map data objects.
        /// </summary>
        /// <remarks>The method associates each parsed map entry with a name from the provided list, or
        /// generates a default name if the list is exhausted. The returned entries include metadata such as index,
        /// offset, enabled state, size, and raw map data. The caller is responsible for ensuring the binary file format
        /// matches the expected structure.</remarks>
        /// <param name="binFilePath">The path to the binary file containing map data to be read. Must refer to an existing file.</param>
        /// <param name="paramDefPath">The path to the param def xml file</param>
        /// <param name="mapNames">A list of map names to associate with the parsed entries. If there are more entries than names, remaining
        /// entries will be assigned default names.</param>
        /// <returns>A list of MapEntry objects representing the maps found in the binary file. The list may be empty if no valid
        /// entries are found.</returns>
        private List<MapEntry> ReadMapEntries(string binFilePath, string paramDefPath, Dictionary<int,string> mapNames)
        {
            var param = PARAM.Read(binFilePath);

            var paramDef = PARAMDEF.XmlDeserialize(paramDefPath);

            param.ApplyParamdefSomewhatCarefully(paramDef);
            
            var maps = new List<MapEntry>();

            foreach (var row in param.Rows)
            {
                var mapNameId = row.Cells.FirstOrDefault(n => n.InternalName == "mapNameId")?.Value;

                if (mapNameId == null)
                {
                    continue;
                }

                var mapName = mapNames.GetValueOrDefault((int)mapNameId);

                if (mapName == null)
                {
                    continue;
                }
                
                var mapSizeX = (int)(ushort)(row.Cells.FirstOrDefault(n => n.InternalName == "mapSizeX")?.Value ?? 0);
                var mapSizeY = (int)(ushort)(row.Cells.FirstOrDefault(n => n.InternalName == "mapSizeY")?.Value ?? 0);
                var mapEnabled = (byte)(row.Cells.FirstOrDefault(n => n.InternalName == "bFreeBattle")?.Value ?? 0) != 0;

                maps.Add(new MapEntry
                {
                    paramRowId = row.ID,
                    Enabled = mapEnabled,
                    MapName = mapName,
                    MapSizeX = mapSizeX,
                    MapSizeY = mapSizeY,
                });
            }
            
            return maps;
        }


        /// <summary>
        /// Writes the specified map configuration to the provided binary and FMG files.
        /// </summary>
        /// <param name="binFilePath">The path to the binary file where the map configuration will be written. Must refer to an existing file.</param>
        /// <param name="paramDefPath">The path to the param def xml file</param>
        /// <param name="fmgFilePath">The path to the FMG file to update with map information. Must refer to an existing file.</param>
        /// <param name="maps">A list of map entries to be written to the binary and FMG files.</param>
        /// <param name="createBackup">Indicates whether a backup of the binary file should be created before writing. The default value is <see
        /// langword="true"/>.</param>
        /// <param name="reorder">Toggle re-ordering of bin file entries.</param>
        /// <returns>The number of map entries that were changed in the binary file.</returns>
        /// <exception cref="FileNotFoundException">Thrown if <paramref name="binFilePath"/> or <paramref name="fmgFilePath"/> does not refer to an existing
        /// file.</exception>
        public int WriteMapConfiguration(string binFilePath, string paramDefPath, string fmgFilePath, List<MapEntry> maps,
            bool createBackup = true, bool reorder = false)
        {
            if (!File.Exists(fmgFilePath))
            {
                throw new FileNotFoundException($"Fmg file not found: {fmgFilePath}");
            }

            if (!File.Exists(binFilePath))
            {
                throw new FileNotFoundException($"Binary file not found: {binFilePath}");
            }

            if (!reorder)
            {
                WriteMapBinary(binFilePath, paramDefPath, maps, createBackup);
            }
            else
            {
                WriteMapBinaryReordered(binFilePath, paramDefPath, maps, createBackup);
            }

            UpdateFmg(fmgFilePath, maps);

            return 0;
        }

        /// <summary>
        /// Updates the specified binary parameter file by applying map entry changes and optionally creates a backup of
        /// the original file.
        /// </summary>
        /// <remarks>If createBackup is set to true, a backup file with the ".bak" extension is created in
        /// the same directory as the original file, overwriting any existing backup. Only the enabled state of each
        /// specified map entry is updated; other data in the parameter file remains unchanged.</remarks>
        /// <param name="binFilePath">The file path to the binary parameter file to update. Must refer to an existing file.</param>
        /// <param name="paramDefPath">The file path to the parameter definition (PARAMDEF) XML file used to interpret the parameter data. Must
        /// refer to an existing file.</param>
        /// <param name="maps">A list of map entries specifying which parameter rows to update and their enabled state. Each entry
        /// corresponds to a row in the parameter file.</param>
        /// <param name="createBackup">true to create a backup of the original binary file before making changes; otherwise, false.</param>
        private void WriteMapBinary(string binFilePath, string paramDefPath, List<MapEntry> maps, bool createBackup)
        {
            var param = PARAM.Read(binFilePath);

            var paramDef = PARAMDEF.XmlDeserialize(paramDefPath);

            param.ApplyParamdefSomewhatCarefully(paramDef);


            foreach (var map in maps)
            {
                var row = param.Rows.FirstOrDefault(r=>r.ID == map.paramRowId);
                var mapEnabledCell = row.Cells.FirstOrDefault(n => n.InternalName == "bFreeBattle");
                mapEnabledCell?.Value = map.Enabled ? 1 : 0;
            }

            if (createBackup)
            {
                var backupPath = binFilePath + ".bak";
                File.Copy(binFilePath, backupPath, overwrite: true);
            }
            
            param.Write(binFilePath);
        }

        /// <summary>
        /// Updates the specified binary parameter file by applying map entry changes with reordering them in the bin and optionally creates a backup of
        /// the original file.
        /// </summary>
        /// <remarks>If createBackup is set to true, a backup file with the ".bak" extension is created in
        /// the same directory as the original file, overwriting any existing backup. Only the enabled state of each
        /// specified map entry is updated; other data in the parameter file remains unchanged.</remarks>
        /// <param name="binFilePath">The file path to the binary parameter file to update. Must refer to an existing file.</param>
        /// <param name="paramDefPath">The file path to the parameter definition (PARAMDEF) XML file used to interpret the parameter data. Must
        /// refer to an existing file.</param>
        /// <param name="maps">A list of map entries specifying which parameter rows to update and their enabled state. Each entry
        /// corresponds to a row in the parameter file.</param>
        /// <param name="createBackup">true to create a backup of the original binary file before making changes; otherwise, false.</param>
        private void WriteMapBinaryReordered(string binFilePath, string paramDefPath, List<MapEntry> maps, bool createBackup)
        {
            var param = PARAM.Read(binFilePath);
            var paramDef = PARAMDEF.XmlDeserialize(paramDefPath);
            param.ApplyParamdefSomewhatCarefully(paramDef);

            // Stage 1: Update enabled/disabled state
            foreach (var map in maps)
            {
                var row = param.Rows.FirstOrDefault(r => r.ID == map.paramRowId);
                var mapEnabledCell = row?.Cells.FirstOrDefault(n => n.InternalName == "bFreeBattle");
                mapEnabledCell?.Value = map.Enabled ? 1 : 0;
            }

            // Stage 2: Reorder paramRowId based on the order in 'maps'
            // Build a mapping from old ID to new ID (e.g., assign new sequential IDs or a custom order)
            var paramIdsInBin = param.Rows.Select(p => p.ID).Where(id => id != 0).ToList();
            var rowIdChanges = new Dictionary<int, int>();

            var paramIndex = 0;
            foreach (var map in maps)
            {
                // Assign new ID based on position in 'maps'
                var newId = paramIdsInBin[paramIndex];
                rowIdChanges.Add(map.paramRowId, newId);
                paramIndex++;
            }

            // Collect rows to update, keyed by original ID
            var rowsToUpdate = new Dictionary<int, PARAM.Row>();
            foreach (var rowIdChange in rowIdChanges)
            {
                var row = param.Rows.FirstOrDefault(r => r.ID == rowIdChange.Key);
                if (row != null)
                {
                    rowsToUpdate[rowIdChange.Key] = row;
                }
            }

            // First pass: assign temporary IDs to avoid collisions
            var tempIdBase = int.MinValue + 1000; // unlikely to conflict
            foreach (var rowIdChange in rowIdChanges)
            {
                if (rowsToUpdate.TryGetValue(rowIdChange.Key, out var row))
                {
                    row.ID = tempIdBase++;
                }
            }

            // Second pass: assign final new IDs
            paramIndex = 0;
            foreach (var rowIdChange in rowIdChanges)
            {
                if (rowsToUpdate.TryGetValue(rowIdChange.Key, out var row))
                {
                    row.ID = rowIdChange.Value;
                }
                paramIndex++;
            }

            param.Rows = param.Rows.OrderBy(p=>p.ID).ToList();
            if (createBackup)
            {
                var backupPath = binFilePath + ".bak";
                File.Copy(binFilePath, backupPath, overwrite: true);
            }

            param.Write(binFilePath);
        }


        /// <summary>
        /// Updates the specified FMG file by replacing entries in the ID range 1001 to 2000 with enabled map entries
        /// from the provided list.
        /// </summary>
        /// <remarks>Existing FMG entries with IDs between 1001 and 2000 are removed before new enabled
        /// map entries are added, starting from ID 1001. The changes are saved to the same file specified by <paramref
        /// name="fmgFilePath"/>.</remarks>
        /// <param name="fmgFilePath">The path to the FMG file to update. Must refer to a valid FMG file.</param>
        /// <param name="maps">A list of map entries to add to the FMG file. Only entries with the Enabled property set to <see
        /// langword="true"/> are included.</param>
        private void UpdateFmg(string fmgFilePath, List<MapEntry> maps)
        {
            var fmg = FMG.Read(fmgFilePath);

            // Remove old entries in the 1001..2000 range
            fmg.Entries.RemoveAll(e => e.ID is >= 1001 and <= 2000);

            // Add enabled maps starting from ID 1001
            var id = 1001;
            foreach (var map in maps)
            {
                if (!map.Enabled)
                {
                    continue;
                }

                fmg.Entries.Add(new FMG.Entry(id, map.MapName));
                id++;
            }

            // Write back to file
            fmg.Write(fmgFilePath);
        }
    }
}