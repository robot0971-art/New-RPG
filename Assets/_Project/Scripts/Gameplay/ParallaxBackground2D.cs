using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class ParallaxBackground2D : MonoBehaviour
{
    [Serializable]
    private sealed class Layer
    {
        public Transform tile;
        public float speed = 0.25f;

        [NonSerialized] public Transform duplicate;
        [NonSerialized] public float width;
        [NonSerialized] public float startX;
    }

    [SerializeField] private Layer[] layers;
    [SerializeField] private bool autoFindChildLayers = true;
    [SerializeField] private bool startScrolling = true;
    [Header("Scroll Speeds")]
    [SerializeField] private float farSpeed = 0.08f;
    [SerializeField] private float midSpeed = 0.22f;
    [SerializeField] private float groundSpeed = 0.45f;

    private const string LoopSuffix = "_Loop";
    private bool isScrolling;

    private void Reset()
    {
        BuildLayersFromChildren();
    }

    private void OnValidate()
    {
        if (autoFindChildLayers && !Application.isPlaying)
        {
            BuildLayersFromChildren();
        }
    }

    private void Awake()
    {
        isScrolling = startScrolling;
        RemoveRuntimeLoopTiles();

        if (autoFindChildLayers && (layers == null || layers.Length == 0))
        {
            BuildLayersFromChildren();
        }

        CreateDuplicateTiles();
    }

    private void Update()
    {
        if (!isScrolling || layers == null)
        {
            return;
        }

        foreach (var layer in layers)
        {
            if (layer?.tile == null || layer.duplicate == null || layer.width <= 0f)
            {
                continue;
            }

            float delta = layer.speed * Time.deltaTime;
            MoveLeft(layer.tile, delta);
            MoveLeft(layer.duplicate, delta);
            WrapLayer(layer);
        }
    }

    public void SetScrolling(bool value)
    {
        isScrolling = value;
    }

    private void BuildLayersFromChildren()
    {
        var childLayers = new List<Layer>();
        for (int i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i);
            if (child.name.EndsWith(LoopSuffix, StringComparison.Ordinal))
            {
                continue;
            }

            if (child.GetComponent<SpriteRenderer>() == null)
            {
                continue;
            }

            childLayers.Add(new Layer
            {
                tile = child,
                speed = GetDefaultSpeed(child.name)
            });
        }

        layers = childLayers.ToArray();
    }

    private void CreateDuplicateTiles()
    {
        if (layers == null)
        {
            return;
        }

        foreach (var layer in layers)
        {
            if (layer?.tile == null)
            {
                continue;
            }

            layer.width = GetWorldWidth(layer.tile);
            if (layer.width <= 0f)
            {
                continue;
            }

            layer.startX = layer.tile.localPosition.x;
            layer.duplicate = Instantiate(layer.tile, layer.tile.parent);
            layer.duplicate.name = layer.tile.name + LoopSuffix;
            SetLocalX(layer.duplicate, layer.startX + layer.width);
        }
    }

    private void RemoveRuntimeLoopTiles()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (!child.name.EndsWith(LoopSuffix, StringComparison.Ordinal))
            {
                continue;
            }

            Destroy(child.gameObject);
        }
    }

    private static void MoveLeft(Transform tile, float delta)
    {
        var position = tile.localPosition;
        position.x -= delta;
        tile.localPosition = position;
    }

    private static void SetLocalX(Transform tile, float x)
    {
        var position = tile.localPosition;
        position.x = x;
        tile.localPosition = position;
    }

    private static void WrapLayer(Layer layer)
    {
        var left = layer.tile.localPosition.x <= layer.duplicate.localPosition.x
            ? layer.tile
            : layer.duplicate;
        var right = left == layer.tile ? layer.duplicate : layer.tile;

        if (left.localPosition.x <= layer.startX - layer.width)
        {
            SetLocalX(left, right.localPosition.x + layer.width);
        }
    }

    private static float GetWorldWidth(Transform tile)
    {
        var renderer = tile.GetComponent<SpriteRenderer>();
        return renderer == null ? 0f : renderer.bounds.size.x;
    }

    private float GetDefaultSpeed(string layerName)
    {
        string lowerName = layerName.ToLowerInvariant();
        if (lowerName.Contains("far"))
        {
            return farSpeed;
        }

        if (lowerName.Contains("mid"))
        {
            return midSpeed;
        }

        if (lowerName.Contains("ground"))
        {
            return groundSpeed;
        }

        return 0.25f;
    }
}
