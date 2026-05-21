using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

public static class VectorUtils
{
    public static (Vector position, QAngle rotation) GetEndXYZ(CCSPlayerController player, CBaseProp block, double distance = 250, bool grid = false, float gridValue = 0f, bool snapping = false, float snapValue = 0f)
    {
        if (Blocks.Entities.TryGetValue(Building.BuilderHolds[player].Entity, out var locked))
        {
            if (Blocks.Entities[locked.Entity].Properties.Locked)
            {
                if (Building.BuilderHolds[player].LockedMessage == false)
                    Utils.PrintToChat(player, $"{ChatColors.Red}Block is locked");

                Building.BuilderHolds[player].LockedMessage = true;

                return (block.AbsOrigin!, block.AbsRotation!);
            }
        }

        var pawn = player.Pawn()!;
        var playerpos = pawn.AbsOrigin!;

        Vector aim = new(playerpos.X, playerpos.Y, playerpos.Z + pawn.ViewOffset.Z); 

        double angleA = -pawn.EyeAngles.X;
        double angleB = pawn.EyeAngles.Y;
        double radianA = (Math.PI / 180) * angleA;
        double radianB = (Math.PI / 180) * angleB;
        double x = aim.X + distance * Math.Cos(radianA) * Math.Cos(radianB);
        double y = aim.Y + distance * Math.Cos(radianA) * Math.Sin(radianB);
        double z = aim.Z + distance * Math.Sin(radianA);

        if (grid && gridValue != 0)
        {
            x = (float)Math.Round(x / gridValue) * gridValue;
            y = (float)Math.Round(y / gridValue) * gridValue;
            z = (float)Math.Round(z / gridValue) * gridValue;
        }

        Vector endPos = new((float)x, (float)y, (float)z);
        QAngle endRotation = block.AbsRotation!;

        if (snapping)
        {
            float scale = Blocks.Entities.ContainsKey(block) ? Utils.GetSize(Blocks.Entities[block].Size) : 1;

            var closestBlock = Utils.GetClosestBlock(endPos, block, scale * block.Collision.Maxs.X * 2);
            if (closestBlock != null)
            {
                var snap = SnapToClosestBlock(block, closestBlock, snapValue, endPos);

                endPos = snap.Position;
                endRotation = snap.Rotation;
            }
        }

        return (endPos, endRotation);
    }

    public static (Vector Position, QAngle Rotation) SnapToClosestBlock(CBaseProp block, CBaseProp closestBlock, float snapValue, Vector playerEyePos)
    {
        Vector position = block.AbsOrigin!;
        QAngle rotation = closestBlock.AbsRotation!;

        float blockScale = Blocks.Entities.ContainsKey(block) ? Utils.GetSize(Blocks.Entities[block].Size) : 1;
        float closestBlockScale = Blocks.Entities.ContainsKey(closestBlock) ? Utils.GetSize(Blocks.Entities[closestBlock].Size) : 1;
        Vector blockDimensions = (block.Collision.Maxs - block.Collision.Mins) * blockScale;
        Vector closestBlockDimensions = (closestBlock.Collision.Maxs - closestBlock.Collision.Mins) * closestBlockScale;

        // Get the forward, right, and up vectors based on the closest block's rotation
        Vector forward = new(
            (float)Math.Cos(rotation.Y * Math.PI / 180) * (float)Math.Cos(rotation.X * Math.PI / 180),
            (float)Math.Sin(rotation.Y * Math.PI / 180) * (float)Math.Cos(rotation.X * Math.PI / 180),
            (float)-Math.Sin(rotation.X * Math.PI / 180)
        );
        Vector right = new(
            (float)Math.Cos((rotation.Y + 90) * Math.PI / 180),
            (float)Math.Sin((rotation.Y + 90) * Math.PI / 180),
            0
        );
        Vector up = Cross(forward, right);

        // Calculate face directions
        Vector[] faceDirections =
        {
            -forward,  // -X face
            forward,   // +X face
            -right,    // -Y face
            right,     // +Y face
            -up,       // -Z face
            up         // +Z face
        };

        // Calculate face centers for the closest block (edge positions)
        Vector[] closestBlockFaceCenters = new Vector[6];
        Vector[] blockFaceOffsets = new Vector[6];

        for (int i = 0; i < 6; i++)
        {
            int axis = i / 2; // 0=X, 1=Y, 2=Z
            bool isMinFace = i % 2 == 0; // Even indices are min faces (-X, -Y, -Z)

            // Calculate closest block's face center (at its edge)
            Vector closestBlockFaceCenter = closestBlock.AbsOrigin!;
            if (axis == 0) // X axis
                closestBlockFaceCenter += faceDirections[i] * closestBlockDimensions.X * 0.5f;
            else if (axis == 1) // Y axis
                closestBlockFaceCenter += faceDirections[i] * closestBlockDimensions.Y * 0.5f;
            else // Z axis
                closestBlockFaceCenter += faceDirections[i] * closestBlockDimensions.Z * 0.5f;

            closestBlockFaceCenters[i] = closestBlockFaceCenter;

            // Calculate our block's offset (placing our edge against their edge)
            if (axis == 0) // X axis
                blockFaceOffsets[i] = faceDirections[i] * blockDimensions.X * 0.5f;
            else if (axis == 1) // Y axis
                blockFaceOffsets[i] = faceDirections[i] * blockDimensions.Y * 0.5f;
            else // Z axis
                blockFaceOffsets[i] = faceDirections[i] * blockDimensions.Z * 0.5f;
        }

        float closestDistance = float.MaxValue;
        int closestFace = -1;

        for (int i = 0; i < closestBlockFaceCenters.Length; i++)
        {
            float distance = CalculateDistance(playerEyePos, closestBlockFaceCenters[i]);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestFace = i;
                position = closestBlockFaceCenters[i] + blockFaceOffsets[i];
            }
        }

        // Apply additional snap value if needed (for gap between blocks)
        if (closestFace != -1 && snapValue != 0)
            position += faceDirections[closestFace] * snapValue;

        return (position, rotation);
    }

    public static Vector Cross(Vector a, Vector b)
    {
        return new(
            a.Y * b.Z - a.Z * b.Y,
            a.Z * b.X - a.X * b.Z,
            a.X * b.Y - a.Y * b.X
        );
    }

    public static float Dot(Vector a, Vector b)
    {
        return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
    }

    public static float CalculateDistance(Vector a, Vector b)
    {
        return (float)Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2) + Math.Pow(a.Z - b.Z, 2));
    }

    public static bool IsWithinBounds(Vector entityPosition, Vector playerPosition, Vector entitySize, Vector playerSize)
    {
        bool overlapX = Math.Abs(entityPosition.X - playerPosition.X) <= (entitySize.X + playerSize.X) / 2;
        bool overlapY = Math.Abs(entityPosition.Y - playerPosition.Y) <= (entitySize.Y + playerSize.Y) / 2;
        bool overlapZ = Math.Abs(entityPosition.Z - playerPosition.Z) <= (entitySize.Z + playerSize.Z) / 2;

        return overlapX && overlapY && overlapZ;
    }

    public static bool CheckOnTop(Blocks.Data block, CCSPlayerPawn pawn)
    {
        Vector playerMaxs = pawn.Collision.Maxs * 2;
        Vector blockMaxs = block.Entity.Collision.Maxs * Utils.GetSize(block.Size) * 2;

        Vector blockOrigin = block.Entity.AbsOrigin!;
        Vector pawnOrigin = pawn.AbsOrigin!;
        QAngle blockRotation = block.Entity.AbsRotation!;

        if (!IsTopOnly(blockOrigin, pawnOrigin, blockMaxs, playerMaxs, blockRotation))
            return false;

        return true;
    }
    public static bool IsTopOnly(Vector entityPosition, Vector playerPosition, Vector entitySize, Vector playerSize, QAngle entityRotation)
    {
        Vector forward = new(
            (float)(Math.Cos(entityRotation.Y * Math.PI / 180) * Math.Cos(entityRotation.X * Math.PI / 180)),
            (float)(Math.Sin(entityRotation.Y * Math.PI / 180) * Math.Cos(entityRotation.X * Math.PI / 180)),
            (float)(-Math.Sin(entityRotation.X * Math.PI / 180))
        );
        Vector right = new(
            (float)(Math.Cos((entityRotation.Y + 90) * Math.PI / 180)),
            (float)(Math.Sin((entityRotation.Y + 90) * Math.PI / 180)),
            0
        );
        Vector up = Cross(forward, right);

        Vector[] faceDirections =
        {
            -forward,  // -X face
            forward,   // +X face
            -right,    // -Y face
            right,     // +Y face
            -up,       // -Z face
            up         // +Z face
        };

        // Find the face with the most positive Z-component (most "upward")
        int topFaceIndex = 0;
        float maxZ = float.MinValue;
        for (int i = 0; i < faceDirections.Length; i++)
        {
            if (faceDirections[i].Z > maxZ)
            {
                maxZ = faceDirections[i].Z;
                topFaceIndex = i;
            }
        }

        Vector topFaceNormal = faceDirections[topFaceIndex];
        Vector localX, localY, localZ;
        float faceWidth, faceHeight, faceDepth;

        // Map the face to its local coordinate system and dimensions
        switch (topFaceIndex)
        {
            case 0: // -X face
            case 1: // +X face
                    // For +X face: faceWidth along Y (right), faceHeight along Z (up)
                localX = up;    // Map Z-axis (up) to faceHeight
                localY = right; // Map Y-axis (right) to faceWidth
                localZ = topFaceNormal;
                faceWidth = entitySize.Y;  // Y-axis (right)
                faceHeight = entitySize.Z; // Z-axis (up)
                faceDepth = entitySize.X;  // X-axis (forward)
                break;
            case 2: // -Y face
            case 3: // +Y face
                    // For +Y face: faceWidth along X (forward), faceHeight along Z (up)
                localX = up;     // Map Z-axis (up) to faceHeight
                localY = forward; // Map X-axis (forward) to faceWidth
                localZ = topFaceNormal;
                faceWidth = entitySize.X;  // X-axis (forward)
                faceHeight = entitySize.Z; // Z-axis (up)
                faceDepth = entitySize.Y;  // Y-axis (right)
                break;
            case 4: // -Z face
            case 5: // +Z face
            default:
                // For +Z face: faceWidth along X (right), faceHeight along Y (forward)
                localX = right;  // Map X-axis (right) to faceWidth
                localY = forward; // Map Y-axis (forward) to faceHeight
                localZ = topFaceNormal;
                faceWidth = entitySize.X;  // X-axis (right)
                faceHeight = entitySize.Y; // Y-axis (forward)
                faceDepth = entitySize.Z;  // Z-axis (up)
                break;
        }

        // Calculate the center of the top face
        Vector topFaceCenter = entityPosition + topFaceNormal * (faceDepth / 2);

        // Player position relative to the top face center
        Vector relativePos = playerPosition - topFaceCenter;

        // Project relative position onto the block's local axes
        float localXProj = Dot(relativePos, localX); // Along local X (should map to faceHeight)
        float localYProj = Dot(relativePos, localY); // Along local Y (should map to faceWidth)
        float localZProj = Dot(relativePos, localZ); // Along local Z (normal)

        // Boundary checks with dynamic tolerance based on player size
        float boundaryToleranceX = playerSize.X / 2 + 2.0f; // Account for player's collision box width
        float boundaryToleranceY = playerSize.Y / 2 + 2.0f; // Account for player's collision box height
        float triggerThreshold = 2.0f;
        float zTolerance = 0.1f; // Small tolerance for Z-axis
        bool overlapX = Math.Abs(localXProj) <= (faceHeight / 2) + boundaryToleranceX; // localXProj maps to faceHeight
        bool overlapY = Math.Abs(localYProj) <= (faceWidth / 2) + boundaryToleranceY;  // localYProj maps to faceWidth
        bool onTop = localZProj >= -zTolerance && localZProj <= (playerSize.Z + triggerThreshold);

        // Additional logging for debugging
        /*Console.WriteLine($"entityPosition: {entityPosition}, playerPosition: {playerPosition}, entityRotation: {entityRotation}");
        Console.WriteLine($"topFaceCenter: {topFaceCenter}, relativePos: {relativePos}");
        Console.WriteLine($"entitySize: {entitySize}, playerSize: {playerSize}");
        Console.WriteLine($"TopFace: {topFaceIndex}, localXProj: {localXProj}, localYProj: {localYProj}, localZProj: {localZProj}");
        Console.WriteLine($"faceWidth: {faceWidth}, faceHeight: {faceHeight}, overlapX: {overlapX}, overlapY: {overlapY}, onTop: {onTop}");*/

        return overlapX && overlapY && onTop;
    }

    public class VectorDTO
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public VectorDTO() { }

        public VectorDTO(Vector vector)
        {
            X = vector.X;
            Y = vector.Y;
            Z = vector.Z;
        }

        public Vector ToVector() => new(X, Y, Z);
    }

    public class QAngleDTO
    {
        public float Pitch { get; set; }
        public float Yaw { get; set; }
        public float Roll { get; set; }

        public QAngleDTO() { }

        public QAngleDTO(QAngle qangle)
        {
            Pitch = qangle.X;
            Yaw = qangle.Y;
            Roll = qangle.Z;
        }

        public QAngle ToQAngle() => new(Pitch, Yaw, Roll);
    }
}