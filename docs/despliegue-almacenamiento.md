# Despliegue del almacenamiento multi-ubicación (Raspberry Pi)

Pasos que hay que hacer **en el host** (la Pi) para que la API use los discos con las protecciones del módulo de
almacenamiento. Nada de esto lo hace el código: sin estos pasos la API arranca, pero no está protegida contra el
fallo que el módulo quiere evitar (escribir en el M.2 creyendo que se escribe en un HDD que no está montado).

## Qué espera la configuración

Valores actuales de [`docker-compose.yml`](../docker-compose.yml) y [`HomeDB/appsettings.json`](../HomeDB/appsettings.json):

| Clave de disco | Host (Pi) | Contenedor | Política | Ubicaciones |
| --- | --- | --- | --- | --- |
| `m2-1` | `/srv/homedb/storage` | `/storage` | `Required` | `main` (Personal, `files`), `temp` (Temp, `temp-uploads`) |
| `hdd1` | `/mnt/hdd1` | `/das/hdd1` | `Optional` | `media` (Media, `media`) |
| `hdd2` | `/mnt/hdd2` | `/das/hdd2` | `Optional` | `backups` (Backups, `backups`) |

Los logs de respaldo van a `/srv/homedb/logs` (host) → `/logs` (contenedor).

> **Los puntos de montaje `/mnt/hdd1` y `/mnt/hdd2` son una suposición** (salen del ejemplo del README de Notion).
> Si montas los discos en otra ruta, cambia el lado izquierdo de los volumes en `docker-compose.yml`.

## Regla del marcador

Cada disco lleva en su raíz un fichero `.homedb-location` cuyo contenido es **exactamente la clave del disco**
(`m2-1`, `hdd1`, `hdd2`). Un salto de línea al final no importa; mayúsculas/minúsculas tampoco. La API lo comprueba
al arrancar, cada 60 s y en cada operación sobre archivos. Si falta o no coincide, el disco se considera no disponible
y las operaciones que lo necesitan responden **503** con el código `1028` (`StorageLocationUnavailable`).

El marcador identifica al disco, no a la carpeta: si formateas o sustituyes un disco, hay que volver a crearlo.

## 1. Preparar el M.2 (una sola vez)

```bash
sudo mkdir -p /srv/homedb/storage /srv/homedb/logs
echo m2-1 | sudo tee /srv/homedb/storage/.homedb-location
```

Las carpetas hay que crearlas antes: el compose usa `create_host_path: false`, así que si no existen el contenedor
no arranca (a propósito, para que Docker no las cree con un propietario inesperado).

## 2. Preparar cada HDD (una sola vez por disco)

El orden importa: **crear el punto de montaje → protegerlo → montar → marcar**.

1. Identificar el disco y su UUID (los nombres `/dev/sdX` pueden cambiar entre arranques, el UUID no):

   ```bash
   lsblk -f
   ```

   Se recomienda `ext4`. Otros sistemas de ficheros (NTFS, exFAT) no admiten `chattr` ni permisos de Linux.

2. Crear los puntos de montaje y marcarlos como inmutables **mientras están vacíos y sin montar**:

   ```bash
   sudo mkdir -p /mnt/hdd1 /mnt/hdd2
   sudo chattr +i /mnt/hdd1 /mnt/hdd2
   lsattr -d /mnt/hdd1 /mnt/hdd2
   ```

   `lsattr` debe mostrar una `i` en cada línea. Con el atributo puesto, si el disco no está montado nadie (ni la API)
   puede escribir dentro de la carpeta vacía. El atributo se queda en la carpeta, no en el disco, y deja de verse
   cuando el disco se monta encima.

3. Añadir los discos a `/etc/fstab`, con `nofail` para que un disco ausente no bloquee el arranque de la Pi:

   ```
   UUID=<uuid-del-hdd1>  /mnt/hdd1  ext4  defaults,nofail,noatime,x-systemd.device-timeout=10  0  2
   UUID=<uuid-del-hdd2>  /mnt/hdd2  ext4  defaults,nofail,noatime,x-systemd.device-timeout=10  0  2
   ```

4. Montar y comprobar:

   ```bash
   sudo mount -a
   findmnt /mnt/hdd1 /mnt/hdd2
   ```

5. Crear el marcador de cada disco (una vez montado):

   ```bash
   echo hdd1 | sudo tee /mnt/hdd1/.homedb-location
   echo hdd2 | sudo tee /mnt/hdd2/.homedb-location
   ```

## 3. Arrancar

```bash
docker compose up -d --build
docker logs homedb-api --tail 50
```

Si algo está mal con un disco `Required` (`m2-1`), el contenedor termina con un mensaje que empieza por
`No se puede arrancar: hay discos obligatorios (StartupPolicy=Required) no disponibles` e indica el disco, su ruta y
el motivo. Si falla un disco `Optional`, la API arranca y aparece un aviso `Critical` en la pantalla de Registros.

## 4. Verificación

Hacerlo una vez tras el despliegue. Son las únicas pruebas que ejercitan discos reales: los tests automáticos
simulan un disco caído borrando el marcador.

### 4.1 El contenedor ve los discos y sus marcadores

```bash
docker exec homedb-api ls -la /storage /das/hdd1 /das/hdd2
```

Cada listado debe incluir `.homedb-location`.

### 4.2 Un HDD que se desmonta se detecta, y uno que vuelve se ve sin reiniciar

Esto comprueba `propagation: rslave` (que el contenedor vea un montaje hecho **después** de arrancar):

```bash
sudo umount /mnt/hdd1
# Esperar hasta 60 s: en Registros debe aparecer un Critical "ha dejado de estar disponible" para hdd1
sudo mount /mnt/hdd1
docker exec homedb-api ls -la /das/hdd1
# Esperar hasta 60 s: en Registros debe aparecer un Information "vuelve a estar disponible"
```

Si tras volver a montar el contenedor sigue viendo la carpeta vacía, la propagación no funciona: reiniciar el
contenedor lo arregla (`docker compose restart api`), pero hay que investigar la causa (ver la tabla de abajo).

### 4.3 El arranque sin un HDD funciona (política `Optional`)

Con el contenedor parado y el HDD desmontado (`sudo umount /mnt/hdd1`), el punto de montaje existe y está vacío,
así que el contenedor **arranca igualmente**:

```bash
docker compose up -d
docker logs homedb-api --tail 20      # sin errores de arranque
# En Registros: Critical "El disco opcional 'hdd1' no está disponible al arrancar (marker file is missing)"
sudo mount /mnt/hdd1
```

### 4.4 Un disco `Required` sin marcador impide arrancar

```bash
docker compose stop api
sudo mv /srv/homedb/storage/.homedb-location /srv/homedb/storage/.homedb-location.bak
docker compose up api         # debe terminar con el error de StartupPolicy=Required
sudo mv /srv/homedb/storage/.homedb-location.bak /srv/homedb/storage/.homedb-location
docker compose up -d
```

## Qué verás cuando un disco falla

| Situación | Comportamiento |
| --- | --- |
| Disco `Required` ausente al arrancar | La API no arranca. Mensaje en `docker logs` (el `Critical` no se persiste: la API no llega a arrancar). |
| Disco `Optional` ausente al arrancar | La API arranca. `Critical` en Registros. |
| Un disco se cae con la API en marcha | Operaciones que lo usan: 503 con `ErrorCode` 1028. `Critical` en Registros en ≤ 60 s, un recordatorio cada 24 h mientras siga caído, y un `Information` cuando vuelve. |
| Subida en curso con el disco temporal o personal caído | Iniciar la subida o enviar un chunk da 503. Si cae justo al completar, la sesión termina `Cancelled` con el código 1028. |
| Borrar un archivo con su disco caído | 503; ni el archivo ni su registro se tocan. |

El intervalo de comprobación es `Storage:AvailabilityCheckIntervalSeconds` (60 por defecto, entre 5 y 3600).

## Problemas conocidos y límites

| Síntoma | Causa probable |
| --- | --- |
| `docker compose up` falla con `bind source path does not exist` | Falta crear la carpeta del host (`/srv/homedb/...` o `/mnt/hddN`). |
| Error de Docker sobre `rslave` / "is not a shared or slave mount" | El sistema de ficheros raíz del host no es `shared`. Comprobarlo con `findmnt -o TARGET,PROPAGATION /` (en Raspberry Pi OS con systemd debería serlo). |
| El disco se ve vacío dentro del contenedor tras montarlo con la API en marcha | La propagación de montajes no llegó al contenedor. Reiniciar el contenedor y revisar el punto anterior. |
| Un HDD se remonta en solo lectura tras un error del dock USB | La API lo detecta en el siguiente ciclo (sonda de escritura, ≤ 60 s). Hasta entonces una escritura fallará con un error 500 en vez de 503. |
| Los archivos creados por la API aparecen como propiedad de `root` en el host | Esperado: el contenedor corre como root (el Dockerfile no define `USER`). Para tocarlos a mano hace falta `sudo`. |

Límites del diseño:

- La comprobación por operación lee el marcador (rápido); la de escritura se refresca cada ciclo, no en cada operación.
- No hay comprobación de espacio libre ni límites (`MinFreeBytes`, `MaxUsedBytes`) todavía: están en la configuración pero
  nada los aplica.
- Los backups siguen usando `Backup:Daily:Directory` y `Backup:SourceDirectory` como rutas literales; aún no leen la
  configuración de ubicaciones.
