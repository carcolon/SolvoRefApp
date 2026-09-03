import { useEffect, useMemo, useRef, useState } from 'react';
import { parseLayoutJson } from '../CardStudio/cardStudioTemplates';

function buildGradient(stops, fallback) {
    if (!Array.isArray(stops) || stops.length < 4) {
        return fallback || undefined;
    }

    const pairs = [];
    for (let index = 0; index < stops.length - 1; index += 2) {
        const stop = Number(stops[index]);
        const color = stops[index + 1];
        if (!Number.isFinite(stop) || typeof color !== 'string') {
            continue;
        }
        pairs.push(`${color} ${Math.round(stop * 100)}%`);
    }

    return pairs.length >= 2 ? `linear-gradient(135deg, ${pairs.join(', ')})` : fallback || undefined;
}

function getWeight(fontStyle, fallback = 500) {
    if (fontStyle === 'bold') {
        return 700;
    }

    if (fontStyle === 'semibold') {
        return 600;
    }

    return fallback;
}

export function getManagedCardPresentation(card) {
    let rawLayout = null;
    if (card.layoutJson) {
        try {
            rawLayout = typeof card.layoutJson === 'string' ? JSON.parse(card.layoutJson) : card.layoutJson;
        } catch {
            rawLayout = null;
        }
    }

    const layout = parseLayoutJson(card.layoutJson, card.section);
    const surface = rawLayout?.card || {};
    const title = rawLayout?.elements?.title || {};
    const summary = rawLayout?.elements?.summary || {};
    const date = rawLayout?.elements?.date || {};
    const button = rawLayout?.elements?.button || {};
    const badge = rawLayout?.elements?.badge || {};

    return {
        layout,
        surfaceStyle: {
            background: buildGradient(surface.gradientStops, surface.fill),
            borderColor: surface.stroke || undefined,
            borderWidth: surface.stroke ? '1px' : undefined,
            borderStyle: surface.stroke ? 'solid' : undefined,
            borderRadius: Number.isFinite(surface.radius) ? `${surface.radius}px` : undefined,
        },
        badgeStyle: {
            background: badge.backgroundFill || undefined,
            color: badge.textColor || badge.fill || undefined,
            borderRadius: Number.isFinite(badge.radius) ? `${badge.radius}px` : undefined,
            fontFamily: badge.fontFamily || undefined,
            fontWeight: getWeight(badge.fontStyle, 700),
        },
        titleStyle: {
            color: title.fill || undefined,
            fontFamily: title.fontFamily || undefined,
            fontWeight: getWeight(title.fontStyle, 700),
        },
        bodyStyle: {
            color: summary.fill || undefined,
            fontFamily: summary.fontFamily || undefined,
            fontWeight: getWeight(summary.fontStyle, 500),
        },
        dateStyle: {
            color: date.fill || undefined,
            fontFamily: date.fontFamily || undefined,
            fontWeight: getWeight(date.fontStyle, 500),
        },
        buttonStyle: {
            background: button.backgroundFill || undefined,
            color: button.textColor || button.fill || undefined,
            borderRadius: Number.isFinite(button.radius) ? `${button.radius}px` : undefined,
            fontFamily: button.fontFamily || undefined,
            fontWeight: getWeight(button.fontStyle, 700),
        },
    };
}

export default function PublicManagedCardDecor({ card }) {
    const layout = useMemo(() => parseLayoutJson(card.layoutJson, card.section), [card.layoutJson, card.section]);
    const hostRef = useRef(null);
    const [hostWidth, setHostWidth] = useState(0);

    useEffect(() => {
        if (!hostRef.current || typeof ResizeObserver === 'undefined') {
            return undefined;
        }

        const updateWidth = () => {
            if (!hostRef.current) {
                return;
            }
            setHostWidth(hostRef.current.clientWidth || 0);
        };

        updateWidth();

        const observer = new ResizeObserver(updateWidth);
        observer.observe(hostRef.current);

        return () => observer.disconnect();
    }, []);

    const frame = layout?.card || {};
    const scale = hostWidth > 0 && frame.width ? hostWidth / frame.width : 1;
    const customTextLayers = (layout?.customElements || [])
        .filter((item) => item.visible !== false && (item.sourceKey === 'freeText' || !item.sourceKey) && item.text);

    if (!customTextLayers.length) {
        return null;
    }

    return (
        <div ref={hostRef} className="home-managed-card-decor" aria-hidden="true">
            {customTextLayers.map((item) => (
                <div
                    key={item.id}
                    className="home-managed-card-decor__text"
                    style={{
                        left: `${(item.x - (frame.x || 0)) * scale}px`,
                        top: `${(item.y - (frame.y || 0)) * scale}px`,
                        width: `${(item.width || 0) * scale}px`,
                        minHeight: `${(item.height || 0) * scale}px`,
                        color: item.fill || '#0B2135',
                        fontFamily: item.fontFamily || 'Poppins',
                        fontWeight: getWeight(item.fontStyle, 500),
                        fontSize: `${(item.fontSize || 18) * scale}px`,
                        lineHeight: item.lineHeight || 1.2,
                        opacity: item.opacity ?? 1,
                    }}
                >
                    {item.text}
                </div>
            ))}
        </div>
    );
}
