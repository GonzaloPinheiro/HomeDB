using System.ComponentModel.DataAnnotations;
using System.Linq;
using HomeDB.Domain.Common;

namespace HomeDB.Application.Options
{
    public class StorageOptions : IValidatableObject
    {
        [Required]
        [Range(1, long.MaxValue, ErrorMessage = "El tamaño máximo de archivo debe ser un valor positivo.")]
        public long MaxFileSizeBytes { get; set; }

        [Range(1, long.MaxValue, ErrorMessage = "El límite de almacenamiento debe ser un valor positivo.")]
        public long? StorageLimitBytes { get; set; } = null;

        //Cada cuántos segundos el vigilante en segundo plano vuelve a comprobar todos los discos (marcador y escritura).
        [Range(5, 3600, ErrorMessage = "El intervalo de comprobación de discos debe estar entre 5 y 3600 segundos.")]
        public int AvailabilityCheckIntervalSeconds { get; set; } = 60;

        //Discos físicos. Cada uno agrupa varias ubicaciones lógicas que comparten disponibilidad y política de arranque.
        [MinLength(1, ErrorMessage = "Debe declararse al menos un disco.")]
        public List<DiskOptions> Disks { get; set; } = new();

        //ValidateDataAnnotations() no valida recursivamente listas ni objetos anidados, así que Disks y sus
        //Locations se validan aquí a mano, junto con las reglas que no son de atributo (claves únicas).
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            List<ValidationResult> results = new();

            //Validar los atributos de DataAnnotations de StorageOptions.
            HashSet<string> diskKeys = new(StringComparer.OrdinalIgnoreCase);
            HashSet<string> locationKeys = new(StringComparer.OrdinalIgnoreCase);
            Dictionary<LocationUsage, int> defaultsPerUsage = new();

            //Validar cada disco y sus ubicaciones, y comprobar que las claves sean únicas.
            for (int diskIndex = 0; diskIndex < Disks.Count; diskIndex++)
            {
                DiskOptions disk = Disks[diskIndex];

                //Validar el disco y sus atributos de DataAnnotations.
                List<ValidationResult> diskResults = new();
                Validator.TryValidateObject(disk, new ValidationContext(disk), diskResults, validateAllProperties: true);

                //Agregar el índice y la clave del disco a cada error de validación para que sea más fácil de localizar.
                foreach (ValidationResult diskResult in diskResults)
                    results.Add(new ValidationResult($"Disks[{diskIndex}] ({disk.Key}): {diskResult.ErrorMessage}"));

                //Las claves de disco son únicas en toda la configuración, no solo dentro de su lista.
                if (!string.IsNullOrEmpty(disk.Key) && !diskKeys.Add(disk.Key))
                    results.Add(new ValidationResult($"La clave de disco '{disk.Key}' está duplicada."));

                //Verificar que el campo contenga un valor válido del enum DiskStartupPolicy.
                if (!Enum.IsDefined(typeof(DiskStartupPolicy), disk.StartupPolicy))
                    results.Add(new ValidationResult($"Disks[{diskIndex}] ({disk.Key}): StartupPolicy es obligatorio."));

                //Validar cada ubicación del disco y sus atributos de DataAnnotations.
                for (int locationIndex = 0; locationIndex < disk.Locations.Count; locationIndex++)
                {
                    StorageLocationOptions location = disk.Locations[locationIndex];

                    //Validar la ubicación y sus atributos de DataAnnotations.
                    List<ValidationResult> locationResults = new();
                    Validator.TryValidateObject(location, new ValidationContext(location), locationResults, validateAllProperties: true);

                    //Agregar el índice y la clave de la ubicación a cada error de validación para que sea más fácil de localizar.
                    foreach (ValidationResult locationResult in locationResults)
                        results.Add(new ValidationResult($"Disks[{diskIndex}].Locations[{locationIndex}] ({location.Key}): {locationResult.ErrorMessage}"));

                    //Verificar que el campo contenga un valor válido del enum LocationUsage.
                    if (!Enum.IsDefined(typeof(LocationUsage), location.Usage))
                        results.Add(new ValidationResult($"Disks[{diskIndex}].Locations[{locationIndex}] ({location.Key}): Usage es obligatorio."));
                    else if (location.IsDefault)
                    {
                        defaultsPerUsage.TryGetValue(location.Usage, out int count);
                        defaultsPerUsage[location.Usage] = count + 1;
                    }

                    //Una ubicación de backups no puede entrar en el backup: la copia se guarda dentro de ella y se copiaría a sí misma.
                    if (location.Usage == LocationUsage.Backups && location.IncludeInBackup)
                        results.Add(new ValidationResult($"Disks[{diskIndex}].Locations[{locationIndex}] ({location.Key}): una ubicación de uso Backups no puede tener IncludeInBackup."));

                    //MaxUsedBytes se mide sumando los archivos registrados en la ubicación (FileItems), que solo existen en las
                    //de uso Personal. En otra ubicación el tope nunca saltaría: se rechaza para no dar una falsa protección.
                    //(Cuando existan los torrents, que registran sus ficheros, habrá que revisar esta regla.)
                    if (location.MaxUsedBytes.HasValue && location.Usage != LocationUsage.Personal)
                        results.Add(new ValidationResult($"Disks[{diskIndex}].Locations[{locationIndex}] ({location.Key}): MaxUsedBytes solo se admite en ubicaciones de uso Personal."));

                    //Las claves de ubicación son únicas en toda la configuración.
                    if (!string.IsNullOrEmpty(location.Key) && !locationKeys.Add(location.Key))
                        results.Add(new ValidationResult($"La clave de ubicación '{location.Key}' está duplicada."));
                }
            }

            //Cada Usage presente en la config debe tener exactamente una ubicación por defecto: ninguna
            //deja al resolver sin saber cuál usar, y más de una deja la elección ambigua.
            foreach (LocationUsage usage in Enum.GetValues<LocationUsage>())
            {
                bool usagePresent = Disks.SelectMany(d => d.Locations).Any(l => l.Usage == usage);
                if (!usagePresent)
                    continue;

                defaultsPerUsage.TryGetValue(usage, out int defaultCount);
                if (defaultCount == 0)
                    results.Add(new ValidationResult($"Usage '{usage}' no tiene ninguna ubicación marcada como IsDefault."));
                else if (defaultCount > 1)
                    results.Add(new ValidationResult($"Usage '{usage}' tiene más de una ubicación marcada como IsDefault."));
            }

            return results;
        }
    }
}