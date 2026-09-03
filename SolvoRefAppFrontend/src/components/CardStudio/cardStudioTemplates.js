import { CONTENT_SECTIONS } from '../../content/homeContentConfig';

export const CARD_STUDIO_CANVAS = {
    width: 1280,
    height: 820,
};

const detailScene = {
    width: CARD_STUDIO_CANVAS.width,
    height: CARD_STUDIO_CANVAS.height,
    mode: 'detail',
};

const detailModal = {
    x: 140,
    y: 76,
    width: 1000,
    height: 650,
    radius: 30,
};

const detailElements = {
    image: {
        x: 198,
        y: 138,
        width: 884,
        height: 252,
        visible: true,
        opacity: 1,
    },
    title: {
        x: 198,
        y: 432,
        width: 760,
        height: 64,
        fontSize: 34,
        lineHeight: 1.1,
        visible: true,
    },
    body: {
        x: 198,
        y: 516,
        width: 760,
        height: 150,
        fontSize: 18,
        lineHeight: 1.45,
        visible: true,
    },
};

const baseCard = {
    x: 170,
    y: 190,
    width: 900,
    height: 430,
    radius: 34,
};

const baseElements = {
    logo: {
        x: 220,
        y: 118,
        width: 132,
        height: 132,
        visible: true,
    },
    image: {
        x: 820,
        y: 220,
        width: 180,
        height: 180,
        opacity: 0.22,
        visible: false,
    },
    badge: {
        x: 220,
        y: 230,
        width: 136,
        height: 36,
        fontSize: 16,
        visible: true,
    },
    title: {
        x: 220,
        y: 282,
        width: 560,
        height: 118,
        fontSize: 44,
        lineHeight: 1.08,
        visible: true,
    },
    summary: {
        x: 220,
        y: 410,
        width: 540,
        height: 108,
        fontSize: 18,
        lineHeight: 1.45,
        visible: true,
    },
    date: {
        x: 220,
        y: 548,
        width: 220,
        height: 38,
        fontSize: 18,
        visible: true,
    },
    button: {
        x: 792,
        y: 530,
        width: 220,
        height: 68,
        fontSize: 22,
        visible: true,
    },
};

export const CARD_STUDIO_LAYERS = [
    { key: 'logo', label: 'Logo' },
    { key: 'image', label: 'Media' },
    { key: 'badge', label: 'Badge' },
    { key: 'title', label: 'Headline' },
    { key: 'summary', label: 'Summary' },
    { key: 'date', label: 'Date' },
    { key: 'button', label: 'CTA' },
];

const TEMPLATE_PRESETS = {
    [CONTENT_SECTIONS.spotlight]: {
        version: 1,
        section: CONTENT_SECTIONS.spotlight,
        scene: {
            width: CARD_STUDIO_CANVAS.width,
            height: CARD_STUDIO_CANVAS.height,
            mode: 'spotlight',
        },
        card: {
            ...baseCard,
            x: 130,
            y: 188,
            width: 910,
            height: 438,
        },
        elements: {
            ...baseElements,
            logo: {
                ...baseElements.logo,
                x: 206,
                y: 118,
            },
            badge: {
                ...baseElements.badge,
                x: 206,
                y: 236,
            },
            title: {
                ...baseElements.title,
                x: 206,
                y: 290,
                width: 570,
                fontSize: 45,
            },
            summary: {
                ...baseElements.summary,
                x: 206,
                y: 420,
                width: 528,
            },
            date: {
                ...baseElements.date,
                x: 206,
                y: 560,
            },
            button: {
                ...baseElements.button,
                x: 782,
                y: 540,
            },
            image: {
                ...baseElements.image,
                x: 800,
                y: 248,
                width: 184,
                height: 184,
                opacity: 0.2,
            },
        },
        customElements: [],
        detail: {
            scene: { ...detailScene },
            modal: { ...detailModal },
            imageUrl: '',
            elements: {
                ...detailElements,
            },
            customElements: [],
        },
    },
    [CONTENT_SECTIONS.programNews]: {
        version: 1,
        section: CONTENT_SECTIONS.programNews,
        scene: {
            width: CARD_STUDIO_CANVAS.width,
            height: CARD_STUDIO_CANVAS.height,
            mode: 'program_news',
        },
        card: {
            ...baseCard,
            x: 196,
            y: 210,
            width: 888,
            height: 396,
        },
        elements: {
            ...baseElements,
            logo: {
                ...baseElements.logo,
                x: 250,
                y: 128,
                visible: false,
            },
            badge: {
                ...baseElements.badge,
                x: 248,
                y: 248,
            },
            title: {
                ...baseElements.title,
                x: 248,
                y: 304,
                width: 610,
                height: 94,
                fontSize: 34,
            },
            summary: {
                ...baseElements.summary,
                x: 248,
                y: 398,
                width: 630,
                height: 82,
                fontSize: 17,
            },
            date: {
                ...baseElements.date,
                x: 248,
                y: 546,
            },
            button: {
                ...baseElements.button,
                x: 882,
                y: 532,
                width: 180,
                height: 60,
                fontSize: 20,
            },
            image: {
                ...baseElements.image,
                x: 906,
                y: 250,
                width: 150,
                height: 150,
                opacity: 0.16,
            },
        },
        customElements: [],
        detail: {
            scene: { ...detailScene },
            modal: { ...detailModal },
            imageUrl: '',
            elements: {
                ...detailElements,
            },
            customElements: [],
        },
    },
};

export function createDefaultLayout(section = CONTENT_SECTIONS.spotlight) {
    return JSON.parse(JSON.stringify(TEMPLATE_PRESETS[section] || TEMPLATE_PRESETS[CONTENT_SECTIONS.spotlight]));
}

export function parseLayoutJson(layoutJson, section = CONTENT_SECTIONS.spotlight) {
    const fallback = createDefaultLayout(section);
    if (!layoutJson) {
        return fallback;
    }

    try {
        const parsed = typeof layoutJson === 'string' ? JSON.parse(layoutJson) : layoutJson;
        return {
            ...fallback,
            ...parsed,
            scene: {
                ...fallback.scene,
                ...(parsed?.scene || {}),
            },
            card: {
                ...fallback.card,
                ...(parsed?.card || {}),
            },
            elements: Object.fromEntries(
                Object.entries(fallback.elements).map(([key, value]) => [
                    key,
                    {
                        ...value,
                        ...(parsed?.elements?.[key] || {}),
                    },
                ]),
            ),
            customElements: Array.isArray(parsed?.customElements)
                ? parsed.customElements.map((item) => ({
                      ...fallback.elements[item?.sourceKey || 'title'],
                      ...item,
                  }))
                : [],
            detail: {
                imageUrl: parsed?.detail?.imageUrl || fallback.detail.imageUrl || '',
                scene: {
                    ...fallback.detail.scene,
                    ...(parsed?.detail?.scene || {}),
                },
                modal: {
                    ...fallback.detail.modal,
                    ...(parsed?.detail?.modal || {}),
                },
                elements: Object.fromEntries(
                    Object.entries(fallback.detail.elements).map(([key, value]) => [
                        key,
                        {
                            ...value,
                            ...(parsed?.detail?.elements?.[key] || {}),
                        },
                    ]),
                ),
                customElements: Array.isArray(parsed?.detail?.customElements)
                    ? parsed.detail.customElements.map((item) => ({
                          ...fallback.detail.elements[item?.sourceKey || 'title'],
                          ...item,
                      }))
                    : [],
            },
        };
    } catch {
        return fallback;
    }
}

export function serializeLayout(layout) {
    return JSON.stringify(layout);
}

export function getLayerMeta(section) {
    return {
        sectionLabel: section === CONTENT_SECTIONS.programNews ? 'Program News' : 'Spotlight',
        sceneMode: section === CONTENT_SECTIONS.programNews ? 'program_news' : 'spotlight',
    };
}
