using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class SimpleAutoTilerHelper
{
    readonly Dictionary<(int SourceId, CellNeighborConnections Connections), List<(Vector2I CellId, int AlternativeId)>> cellNeighborConnections = [];

    public SimpleAutoTilerHelper(TileSet tileSet)
    {
        for (var sourceIdx = 0; sourceIdx < tileSet.GetSourceCount(); ++sourceIdx)
            if (tileSet.GetSource(sourceIdx) is TileSetAtlasSource tileSetAtlasSource)
                for (var tileIdx = 0; tileIdx < tileSetAtlasSource.GetTilesCount(); ++tileIdx)
                {
                    var tileId = tileSetAtlasSource.GetTileId(tileIdx);
                    for (var alterantiveTileIdx = 0; alterantiveTileIdx < tileSetAtlasSource.GetAlternativeTilesCount(tileId); ++alterantiveTileIdx)
                    {
                        var alternativeId = tileSetAtlasSource.GetAlternativeTileId(tileId, alterantiveTileIdx);
                        var tileData = tileSetAtlasSource.GetTileData(tileId, alternativeId);

                        var neighborConnections =
                            testConnection(tileData, TileSet.CellNeighbor.BottomSide)
                            | testConnection(tileData, TileSet.CellNeighbor.BottomLeftCorner)
                            | testConnection(tileData, TileSet.CellNeighbor.BottomRightCorner)
                            | testConnection(tileData, TileSet.CellNeighbor.LeftSide)
                            | testConnection(tileData, TileSet.CellNeighbor.RightSide)
                            | testConnection(tileData, TileSet.CellNeighbor.TopSide)
                            | testConnection(tileData, TileSet.CellNeighbor.TopLeftCorner)
                            | testConnection(tileData, TileSet.CellNeighbor.TopRightCorner);

                        if (!cellNeighborConnections.TryGetValue((sourceIdx, neighborConnections), out var connectionList))
                            cellNeighborConnections[(sourceIdx, neighborConnections)] = connectionList = [];
                        connectionList.Add((tileId, alternativeId));

                        static CellNeighborConnections testConnection(TileData tileData, TileSet.CellNeighbor peeringBit)
                        {
                            if (tileData.IsValidTerrainPeeringBit(peeringBit)
                                && tileData.GetTerrainPeeringBit(peeringBit) >= 0)
                            {
                                return TileSetCellNeighborToCellNeighborConnections(peeringBit);
                            }
                            else
                                return CellNeighborConnections.None;
                        }
                    }
                }
    }

    public void SetCellsConnectedSimple(TileMapLayer layer, IList<(Vector2I cellPosition, int sourceId)> cells)
    {
        var allCells = new HashSet<Vector2I>(cells.Select(x => x.cellPosition));

        foreach (var (cellPosition, sourceId) in cells)
        {
            var connections = CellNeighborConnections.None;

            // add all directions
            if (allCells.Contains(cellPosition + new Vector2I(0, 1)))
                connections |= CellNeighborConnections.Bottom;
            if (allCells.Contains(cellPosition + new Vector2I(-1, 1)))
                connections |= CellNeighborConnections.BottomLeft;
            if (allCells.Contains(cellPosition + new Vector2I(1, 1)))
                connections |= CellNeighborConnections.BottomRight;
            if (allCells.Contains(cellPosition + new Vector2I(-1, 0)))
                connections |= CellNeighborConnections.Left;
            if (allCells.Contains(cellPosition + new Vector2I(1, 0)))
                connections |= CellNeighborConnections.Right;
            if (allCells.Contains(cellPosition + new Vector2I(0, -1)))
                connections |= CellNeighborConnections.Top;
            if (allCells.Contains(cellPosition + new Vector2I(-1, -1)))
                connections |= CellNeighborConnections.TopLeft;
            if (allCells.Contains(cellPosition + new Vector2I(1, -1)))
                connections |= CellNeighborConnections.TopRight;

            // remove corners if both sides aren't connected too
            if ((connections & CellNeighborConnections.BottomLeft) != 0 && ((connections & CellNeighborConnections.Left) == 0 || (connections & CellNeighborConnections.Bottom) == 0))
                connections &= ~CellNeighborConnections.BottomLeft;
            if ((connections & CellNeighborConnections.BottomRight) != 0 && ((connections & CellNeighborConnections.Right) == 0 || (connections & CellNeighborConnections.Bottom) == 0))
                connections &= ~CellNeighborConnections.BottomRight;
            if ((connections & CellNeighborConnections.TopLeft) != 0 && ((connections & CellNeighborConnections.Left) == 0 || (connections & CellNeighborConnections.Top) == 0))
                connections &= ~CellNeighborConnections.TopLeft;
            if ((connections & CellNeighborConnections.TopRight) != 0 && ((connections & CellNeighborConnections.Right) == 0 || (connections & CellNeighborConnections.Top) == 0))
                connections &= ~CellNeighborConnections.TopRight;

            var (cellId, alternativeId) = cellNeighborConnections[(sourceId, connections)][0];
            layer.SetCell(cellPosition, sourceId, cellId, alternativeId);
        }
    }

    [Flags]
    enum CellNeighborConnections
    {
        None = 0,
        Bottom = 1 << 0,
        BottomLeft = 1 << 1,
        BottomRight = 1 << 2,
        Left = 1 << 3,
        Right = 1 << 4,
        Top = 1 << 5,
        TopLeft = 1 << 6,
        TopRight = 1 << 7
    }

    static CellNeighborConnections TileSetCellNeighborToCellNeighborConnections(TileSet.CellNeighbor cellNeighbor) => cellNeighbor switch
    {
        TileSet.CellNeighbor.BottomCorner or TileSet.CellNeighbor.BottomSide => CellNeighborConnections.Bottom,
        TileSet.CellNeighbor.BottomLeftCorner => CellNeighborConnections.BottomLeft,
        TileSet.CellNeighbor.BottomRightCorner => CellNeighborConnections.BottomRight,
        TileSet.CellNeighbor.LeftCorner or TileSet.CellNeighbor.LeftSide => CellNeighborConnections.Left,
        TileSet.CellNeighbor.RightCorner or TileSet.CellNeighbor.RightSide => CellNeighborConnections.Right,
        TileSet.CellNeighbor.TopCorner or TileSet.CellNeighbor.TopSide => CellNeighborConnections.Top,
        TileSet.CellNeighbor.TopLeftCorner => CellNeighborConnections.TopLeft,
        TileSet.CellNeighbor.TopRightCorner => CellNeighborConnections.TopRight,
        _ => throw new ArgumentOutOfRangeException(nameof(cellNeighbor), cellNeighbor, null)
    };

    static Vector2I TileSetCellNeighborToVector2I(TileSet.CellNeighbor cellNeighbor) => cellNeighbor switch
    {
        TileSet.CellNeighbor.BottomCorner or TileSet.CellNeighbor.BottomSide => new Vector2I(0, 1),
        TileSet.CellNeighbor.BottomLeftCorner => new Vector2I(-1, 1),
        TileSet.CellNeighbor.BottomRightCorner => new Vector2I(1, 1),
        TileSet.CellNeighbor.LeftCorner or TileSet.CellNeighbor.LeftSide => new Vector2I(-1, 0),
        TileSet.CellNeighbor.RightCorner or TileSet.CellNeighbor.RightSide => new Vector2I(1, 0),
        TileSet.CellNeighbor.TopCorner or TileSet.CellNeighbor.TopSide => new Vector2I(0, -1),
        TileSet.CellNeighbor.TopLeftCorner => new Vector2I(-1, -1),
        TileSet.CellNeighbor.TopRightCorner => new Vector2I(1, -1),
        _ => throw new ArgumentOutOfRangeException(nameof(cellNeighbor), cellNeighbor, null)
    };
}