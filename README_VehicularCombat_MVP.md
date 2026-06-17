# Vehicular Combat MVP

Scripts base para un prototipo simple de vehicular combat en Unity 6000.4.4f1.

No se crean escena, prefabs ni objetos automaticamente. El objetivo es que puedas armar la arena y cablear referencias desde el Editor.

## Input System

Crea o usa un `InputActionAsset` con un Action Map llamado `Vehicle`.

Acciones esperadas:

| Accion | Tipo | Binding sugerido |
| --- | --- | --- |
| `Accelerate` | Value / Axis o Button | W, Gamepad Right Trigger |
| `Reverse` | Value / Axis o Button | S, Gamepad Left Trigger |
| `Steer` | Value / Axis | 1D Axis A negativo / D positivo, Gamepad Left Stick X |
| `Handbrake` | Button | Space, Gamepad Right Shoulder |
| `Look` | Value / Vector2 | Mouse Delta, Gamepad Right Stick |
| `Fire` | Button | Left Mouse Button, Gamepad Left Shoulder |
| `Restart` | Button | R, Gamepad Start |

En el componente `VehicleInputReader`, asigna cada accion como `InputActionReference`:

- `Accelerate Action Reference`
- `Reverse Action Reference`
- `Steer Action Reference`
- `Handbrake Action Reference`
- `Look Action Reference`
- `Fire Action Reference`
- `Restart Action Reference`

Deja `Enable Actions On Enable` activo salvo que otro componente ya habilite el mismo action map.

## PlayerVehicle sugerido

Jerarquia recomendada:

```text
PlayerVehicle
|-- Body
|-- Visuals
|-- CameraTarget
`-- TurretYawPivot
    `-- TurretPitchPivot
        `-- FirePoint
```

Componentes en `PlayerVehicle`:

- `Rigidbody`
- `Collider`
- `VehicleInputReader`
- `ArcadeVehicleController`
- `OrbitCameraController`
- `TurretAimController`
- `VehicleWeapon`

Configuracion sugerida del `Rigidbody`:

- Mass: `1000`
- Linear Damping: `0.5`
- Angular Damping: `2`
- Use Gravity: activo
- Interpolate: `Interpolate`
- Collision Detection: `Continuous Dynamic`
- Freeze Rotation X y Z

`ArcadeVehicleController` usa `Rigidbody.linearVelocity`, fuerzas fisicas, `FixedUpdate`, movimiento relativo a `transform.forward`, friccion lateral artificial y freno de mano. No clampa la velocidad maxima del Rigidbody: solo deja de sumar fuerza de motor cuando alcanza el limite en esa direccion.

## Camara

Crea dos camaras Cinemachine si queres blend de apuntado:

- `CamaraOrbitalGeneral`
- `CamaraOrbitalApuntado`

Ambas pueden seguir/mirar a `CameraTarget`. Asignales esas referencias al `OrbitCameraController`.

El script rota `CameraTarget` con `Look`. Si `CameraTarget` es hijo del vehiculo, su rotacion mundial se mantiene independiente del yaw del chasis, asi que girar el vehiculo no obliga a la camara a copiarlo.

El cursor se bloquea al iniciar, `Escape` lo desbloquea y click izquierdo lo vuelve a bloquear.

## Torreta y disparo

En `TurretAimController` asigna:

- `Player Camera`
- `Turret Yaw Pivot`
- `Turret Pitch Pivot` opcional
- `Ignored Root`: el root del vehiculo jugador
- `Aim Mask`: capas de arena/objetivos a las que se puede apuntar

En `VehicleWeapon` asigna:

- `Input Reader`
- `Fire Point`
- `Projectile Prefab`
- `Vehicle Rigidbody`
- `Owner Root`
- Muzzle flash opcional
- Impact effect opcional

El proyectil hereda la velocidad del vehiculo:

```csharp
firePoint.forward * projectileSpeed + vehicleRigidbody.linearVelocity
```

## Projectile prefab

Crea un prefab con:

- `VehicleProjectile`
- `Rigidbody`
- `SphereCollider` con `Is Trigger` activo

Valores sugeridos:

- Use Gravity: desactivado
- Collision Detection: `Continuous Speculative`

## Objetivos

Crea cinco objetivos estaticos con:

- Mesh o primitiva visual
- Collider
- `DamageableTarget`

Vida sugerida: `Maximum Health = 3`.

Al morir, el objetivo desaparece y el `GameManager` actualiza el contador.

## GameManager y UI

Crea un `GameManager` en la escena y asigna:

- `VehicleInputReader`
- `Remaining Targets Text` (`TextMeshProUGUI`)
- `Victory Panel` inicialmente desactivado
- `Restart Button` opcional

El texto durante la partida queda:

```text
Objetivos restantes: 5
```

El panel de victoria lo armas en el Canvas con el contenido:

```text
VICTORIA
Todos los objetivos fueron destruidos
Presiona R para reiniciar
```

`R` o Gamepad Start reinician la escena si la accion `Restart` esta configurada.

## Limitaciones conocidas

- No hay IA, enemigos moviles, suspension, WheelColliders, minas, power-ups ni pooling.
- El pitch de la torreta asume que el canon apunta localmente hacia `+Z` y rota sobre el eje local `X`.
- El blend exacto entre camaras depende de la configuracion del `CinemachineBrain`.
- Si no se asignan las `InputActionReference` del `VehicleInputReader`, los scripts no leen ningun fallback de teclado o mouse para gameplay.
