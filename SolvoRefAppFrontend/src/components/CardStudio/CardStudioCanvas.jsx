import { useEffect, useMemo, useRef, useState } from 'react';
import { Group, Image as KonvaImage, Layer, Line, Rect, Stage, Text, Transformer } from 'react-konva';
import { FiArrowDown, FiArrowLeft, FiArrowRight, FiArrowUp, FiCopy, FiCornerUpLeft, FiEye, FiEyeOff, FiLayers, FiMaximize2, FiMinimize2, FiTrash2 } from 'react-icons/fi';
import { getContentIcon } from '../../content/homeContentConfig';
import { resolveContentAssetUrl } from '../../config/api';
import { CARD_STUDIO_CANVAS } from './cardStudioTemplates';

const MIN_SIZE = 32;
const GRID_SIZE = 80;
const VIEWPORT_PADDING = 48;
const DEFAULT_VIEWPORT = {
    width: CARD_STUDIO_CANVAS.width,
    height: 700,
};

const BADGE_STYLE_MAP = {
    default: { fill: '#EEF5F8', text: '#0B2135' },
    mint: { fill: '#DFF7EF', text: '#15785F' },
    teal: { fill: '#D7F4FC', text: '#147085' },
    orange: { fill: '#FBE7DC', text: '#C46F34' },
    update: { fill: '#F7EEC4', text: '#B58A0F' },
    testimony: { fill: '#D6F1FC', text: '#1492A6' },
    campaign: { fill: '#F4DDD0', text: '#C16B34' },
    custom: { fill: '#EEF5F8', text: '#0B2135' },
};

const INLINE_TEXT_LAYER_KEYS = new Set(['badge', 'title', 'summary', 'body', 'date', 'button', 'freeText']);
const TYPOGRAPHY_FALLBACKS = {
    badge: '#0B2135',
    title: '#0B2135',
    summary: '#4F6277',
    body: '#4F6277',
    date: '#8F9FB2',
    button: '#FFFFFF',
    freeText: '#0B2135',
};

function stripHtml(value) {
    const stripped = (value || '')
        .replace(/<br\s*\/?>/gi, '\n')
        .replace(/<\/p>/gi, '\n')
        .replace(/<[^>]+>/g, '')
        .replace(/&nbsp;/gi, ' ')
        .replace(/&amp;/gi, '&')
        .trim();

    if (typeof window === 'undefined') {
        return stripped;
    }

    const textarea = document.createElement('textarea');
    textarea.innerHTML = stripped;
    return textarea.value;
}

function clamp(value, min, max) {
    if (Number.isNaN(value)) {
        return min;
    }

    return Math.min(Math.max(value, min), max);
}

function createViewportToFit(width, height) {
    const availableWidth = Math.max(width - VIEWPORT_PADDING * 2, 320);
    const availableHeight = Math.max(height - VIEWPORT_PADDING * 2, 240);
    const scale = clamp(
        Math.min(availableWidth / CARD_STUDIO_CANVAS.width, availableHeight / CARD_STUDIO_CANVAS.height),
        0.32,
        1.4,
    );

    return {
        scale,
        x: (width - (CARD_STUDIO_CANVAS.width * scale)) / 2,
        y: (height - (CARD_STUDIO_CANVAS.height * scale)) / 2,
    };
}

function createViewportToFocusBounds(width, height, bounds) {
    if (!bounds?.width || !bounds?.height) {
        return createViewportToFit(width, height);
    }

    const availableWidth = Math.max(width - VIEWPORT_PADDING * 2, 280);
    const availableHeight = Math.max(height - VIEWPORT_PADDING * 2, 220);
    const scale = clamp(
        Math.min(availableWidth / bounds.width, availableHeight / bounds.height),
        0.24,
        2.4,
    );
    const centerX = bounds.x + bounds.width / 2;
    const centerY = bounds.y + bounds.height / 2;

    return {
        scale,
        x: width / 2 - (centerX * scale),
        y: height / 2 - (centerY * scale),
    };
}

function normalizeViewport(nextViewport, width, height, options = {}) {
    const { mode = 'strict' } = options;
    const scale = clamp(nextViewport.scale, 0.24, 2.4);
    const scaledWidth = CARD_STUDIO_CANVAS.width * scale;
    const scaledHeight = CARD_STUDIO_CANVAS.height * scale;
    const centeredX = (width - scaledWidth) / 2;
    const centeredY = (height - scaledHeight) / 2;

    if (mode === 'manual') {
        return {
            scale,
            x: nextViewport.x,
            y: nextViewport.y,
        };
    }

    return {
        scale,
        x: scaledWidth <= width
            ? centeredX
            : clamp(nextViewport.x, width - scaledWidth - VIEWPORT_PADDING, VIEWPORT_PADDING),
        y: scaledHeight <= height
            ? centeredY
            : clamp(nextViewport.y, height - scaledHeight - VIEWPORT_PADDING, VIEWPORT_PADDING),
    };
}

function useLoadedImage(src) {
    const [image, setImage] = useState(null);

    useEffect(() => {
        if (!src) {
            setImage(null);
            return;
        }

        let cancelled = false;
        let fallbackElement = null;
        const element = new window.Image();
        const applyLoadedImage = (loadedElement) => {
            if (!cancelled) {
                setImage(loadedElement);
            }
        };

        element.crossOrigin = 'anonymous';
        element.onload = () => applyLoadedImage(element);
        element.onerror = () => {
            fallbackElement = new window.Image();
            fallbackElement.onload = () => applyLoadedImage(fallbackElement);
            fallbackElement.onerror = () => {
                if (!cancelled) {
                    setImage(null);
                }
            };
            fallbackElement.src = src;
        };
        element.src = src;
        return () => {
            cancelled = true;
            element.onload = null;
            element.onerror = null;
            if (fallbackElement) {
                fallbackElement.onload = null;
                fallbackElement.onerror = null;
            }
        };
    }, [src]);

    return image;
}

function getCoverCrop(image, width, height) {
    if (!image?.width || !image?.height || !width || !height) {
        return null;
    }

    const targetRatio = width / height;
    const imageRatio = image.width / image.height;

    if (imageRatio > targetRatio) {
        const cropHeight = image.height;
        const cropWidth = cropHeight * targetRatio;
        return {
            cropX: (image.width - cropWidth) / 2,
            cropY: 0,
            cropWidth,
            cropHeight,
        };
    }

    const cropWidth = image.width;
    const cropHeight = cropWidth / targetRatio;
    return {
        cropX: 0,
        cropY: (image.height - cropHeight) / 2,
        cropWidth,
        cropHeight,
    };
}

function buildImageCrop(item, image) {
    if (!image) {
        return {};
    }

    if (
        Number.isFinite(item.cropX) &&
        Number.isFinite(item.cropY) &&
        Number.isFinite(item.cropWidth) &&
        Number.isFinite(item.cropHeight)
    ) {
        return {
            cropX: item.cropX,
            cropY: item.cropY,
            cropWidth: item.cropWidth,
            cropHeight: item.cropHeight,
        };
    }

    if ((item.imageFit || 'cover') === 'contain') {
        return {};
    }

    return getCoverCrop(image, item.width, item.height) || {};
}

function getImageDrawTransform(item) {
    const scaleX = Number.isFinite(Number(item.imageScaleX)) ? Number(item.imageScaleX) : 1;
    const scaleY = Number.isFinite(Number(item.imageScaleY)) ? Number(item.imageScaleY) : 1;
    const width = Math.max(MIN_SIZE, (item.width || MIN_SIZE) * scaleX);
    const height = Math.max(MIN_SIZE, (item.height || MIN_SIZE) * scaleY);

    return {
        x: Number.isFinite(Number(item.imageOffsetX)) ? Number(item.imageOffsetX) : 0,
        y: Number.isFinite(Number(item.imageOffsetY)) ? Number(item.imageOffsetY) : 0,
        width,
        height,
        rotation: Number.isFinite(Number(item.imageRotation)) ? Number(item.imageRotation) : 0,
    };
}

function applyRoundedClip(ctx, width, height, radius = 24) {
    const nextRadius = Math.max(0, Math.min(radius, Math.min(width, height) / 2));
    ctx.beginPath();
    ctx.moveTo(nextRadius, 0);
    ctx.lineTo(width - nextRadius, 0);
    ctx.quadraticCurveTo(width, 0, width, nextRadius);
    ctx.lineTo(width, height - nextRadius);
    ctx.quadraticCurveTo(width, height, width - nextRadius, height);
    ctx.lineTo(nextRadius, height);
    ctx.quadraticCurveTo(0, height, 0, height - nextRadius);
    ctx.lineTo(0, nextRadius);
    ctx.quadraticCurveTo(0, 0, nextRadius, 0);
    ctx.closePath();
}

function CanvasNode({
    elementKey,
    selected,
    isTransformable,
    element,
    children,
    refs,
    onSelect,
    onChange,
    onDragMove,
    onDragEnd,
    onStartInlineEdit,
    interactive,
}) {
    const groupRef = useRef(null);

    useEffect(() => {
        refs.current[elementKey] = groupRef.current;
        return () => {
            delete refs.current[elementKey];
        };
    }, [elementKey, refs]);

    if (!element?.visible) {
        return null;
    }

    return (
        <Group
            ref={groupRef}
            x={element.x}
            y={element.y}
            rotation={element.rotation || 0}
            draggable={interactive}
            onClick={(event) => {
                if (!interactive) {
                    return;
                }
                event.cancelBubble = true;
                onSelect(elementKey, { append: event.evt?.shiftKey });
            }}
            onTap={(event) => {
                if (!interactive) {
                    return;
                }
                event.cancelBubble = true;
                onSelect(elementKey, { append: event.evt?.shiftKey });
            }}
            onDblClick={(event) => {
                if (!interactive) {
                    return;
                }
                event.cancelBubble = true;
                onStartInlineEdit?.(elementKey);
            }}
            onDblTap={(event) => {
                if (!interactive) {
                    return;
                }
                event.cancelBubble = true;
                onStartInlineEdit?.(elementKey);
            }}
            onDragMove={(event) => interactive && onDragMove?.(elementKey, event, element)}
            onDragEnd={(event) => {
                if (!interactive) {
                    return;
                }
                onDragEnd?.();
                onChange(elementKey, {
                    x: event.target.x(),
                    y: event.target.y(),
                    rotation: event.target.rotation(),
                });
            }}
            onTransformEnd={(event) => {
                if (!interactive) {
                    return;
                }
                if (!isTransformable) {
                    event.target.scaleX(1);
                    event.target.scaleY(1);
                    return;
                }

                const target = event.target;
                const scaleX = target.scaleX();
                const scaleY = target.scaleY();
                target.scaleX(1);
                target.scaleY(1);
                onChange(elementKey, {
                    x: target.x(),
                    y: target.y(),
                    width: Math.max(MIN_SIZE, (element.width || MIN_SIZE) * scaleX),
                    height: Math.max(MIN_SIZE, (element.height || MIN_SIZE) * scaleY),
                    rotation: target.rotation(),
                });
            }}
        >
            {selected ? (
                <Rect
                    x={-8}
                    y={-8}
                    width={(element.width || 0) + 16}
                    height={(element.height || 0) + 16}
                    cornerRadius={22}
                    stroke="#35B9D1"
                    strokeWidth={2}
                    dash={[10, 6]}
                    shadowColor="#35B9D1"
                    shadowBlur={16}
                    shadowOpacity={0.12}
                    listening={false}
                />
            ) : null}
            {children}
        </Group>
    );
}

function getActiveScene(layout, viewMode) {
    if (viewMode === 'detail') {
        return {
            scene: layout.detail?.scene,
            card: layout.detail?.modal,
            elements: layout.detail?.elements || {},
            customElements: layout.detail?.customElements || [],
        };
    }

    return {
        scene: layout.scene,
        card: layout.card,
        elements: layout.elements || {},
        customElements: layout.customElements || [],
    };
}

function getLayerType(elementId, activeScene) {
    if (activeScene?.elements?.[elementId]) {
        return elementId;
    }

    const custom = activeScene?.customElements?.find((item) => item.id === elementId);
    return custom?.sourceKey || 'title';
}

function getElementBounds(element) {
    return {
        x: element.x || 0,
        y: element.y || 0,
        width: element.width || 0,
        height: element.height || 0,
    };
}

function CanvasToolbarButton({ active = false, disabled = false, onClick, children }) {
    return (
        <button
            type="button"
            className={`card-studio-canvas__toolBtn ${active ? 'is-active' : ''}`}
            disabled={disabled}
            onClick={onClick}
        >
            {children}
        </button>
    );
}

function CanvasToolbarIconButton({ title, disabled = false, onClick, children }) {
    return (
        <button
            type="button"
            title={title}
            aria-label={title}
            className="card-studio-canvas__iconBtn"
            disabled={disabled}
            onClick={onClick}
        >
            {children}
        </button>
    );
}

function getItemTextColor(item, fallback) {
    return item.fill || item.textColor || fallback;
}

function getItemBackgroundColor(item, fallback) {
    return item.backgroundFill || item.fill || fallback;
}

export default function CardStudioCanvas({
    card,
    layout,
    viewMode = 'card',
    selectedLayer,
    selectedLayers = [],
    onSelectLayer,
    onLayoutElementChange,
    onDuplicateLayer,
    onDeleteLayer,
    onToggleLayerVisibility,
    interactive = true,
    compact = false,
    onAssetDrop,
    onInlineTextChange,
    onAlignSelection,
    onArrangeSelection,
    onApplyImageFramePreset,
    onNudgeImageCrop,
}) {
    const containerRef = useRef(null);
    const stageShellRef = useRef(null);
    const stageRef = useRef(null);
    const transformerRef = useRef(null);
    const nodeRefs = useRef({});
    const rafGuidesRef = useRef(null);
    const panSessionRef = useRef(null);

    const [viewportRect, setViewportRect] = useState(DEFAULT_VIEWPORT);
    const [measuredStageHeight, setMeasuredStageHeight] = useState(DEFAULT_VIEWPORT.height);
    const [viewport, setViewport] = useState(() => createViewportToFit(DEFAULT_VIEWPORT.width, DEFAULT_VIEWPORT.height));
    const [guides, setGuides] = useState([]);
    const [interactionMode, setInteractionMode] = useState('select');
    const [spacePressed, setSpacePressed] = useState(false);
    const [inlineEditor, setInlineEditor] = useState(null);
    const [dropActive, setDropActive] = useState(false);
    const [manualViewport, setManualViewport] = useState(false);
    const [selectionHudVisible, setSelectionHudVisible] = useState(false);
    const [isPanning, setIsPanning] = useState(false);

    const badgeColors = BADGE_STYLE_MAP[card.badgeVariant] || BADGE_STYLE_MAP.default;
    const logoSrc = resolveContentAssetUrl(getContentIcon(card));
    const activeImageUrl = viewMode === 'detail'
        ? (layout?.detail?.imageUrl || card.imageUrl)
        : card.imageUrl;
    const cardImageSrc = activeImageUrl ? resolveContentAssetUrl(activeImageUrl) : '';
    const logoImage = useLoadedImage(logoSrc);
    const cardImage = useLoadedImage(cardImageSrc);
    const activeScene = useMemo(() => getActiveScene(layout, viewMode), [layout, viewMode]);

    const titleText = card.title || 'Click to edit the headline';
    const summaryText = stripHtml(card.descriptionHtml) || 'Click to edit the supporting summary.';
    const detailTitleText = card.detailTitle || card.title || 'Detail title';
    const detailBodyText = stripHtml(card.detailContentHtml || card.descriptionHtml) || 'Add the pop-up description here.';
    const dateText = card.dateText || 'Date label';
    const buttonText = card.buttonText || 'CTA';
    const stageHeight = compact ? 320 : Math.max(420, Math.round(measuredStageHeight || DEFAULT_VIEWPORT.height));
    const activeCardSurface = activeScene.card || {};
    const cardFill = activeCardSurface.fill || '#FFFFFF';
    const cardStroke = activeCardSurface.stroke || 'rgba(11,33,53,0.06)';
    const cardShadowColor = activeCardSurface.shadowColor || '#0B2135';
    const cardShadowOpacity = activeCardSurface.shadowOpacity ?? 0.12;
    const cardShadowBlur = activeCardSurface.shadowBlur ?? 34;
    const cardShadowOffsetY = activeCardSurface.shadowOffsetY ?? 18;

    const renderableElements = useMemo(() => {
        const base = Object.entries(activeScene.elements || {}).map(([key, value]) => ({
            id: key,
            sourceKey: key,
            ...value,
        }));
        const custom = (activeScene.customElements || []).map((item) => ({
            ...item,
            id: item.id,
            sourceKey: item.sourceKey || item.id,
        }));
        return [...base, ...custom].sort((left, right) => {
            const leftOrder = Number.isFinite(left.zIndex) ? left.zIndex : 0;
            const rightOrder = Number.isFinite(right.zIndex) ? right.zIndex : 0;
            return leftOrder - rightOrder;
        });
    }, [activeScene]);

    const selectedElement = useMemo(
        () => renderableElements.find((item) => item.id === selectedLayer) || null,
        [renderableElements, selectedLayer],
    );
    const activeSelection = selectedLayers.length ? selectedLayers : selectedLayer ? [selectedLayer] : [];
    const selectedElements = useMemo(
        () => renderableElements.filter((item) => activeSelection.includes(item.id)),
        [activeSelection, renderableElements],
    );
    const selectedBounds = useMemo(() => {
        if (!selectedElements.length) {
            return null;
        }

        return selectedElements.reduce((acc, item) => {
            const bounds = getElementBounds(item);
            const left = Math.min(acc.left, bounds.x);
            const top = Math.min(acc.top, bounds.y);
            const right = Math.max(acc.right, bounds.x + bounds.width);
            const bottom = Math.max(acc.bottom, bounds.y + bounds.height);

            return {
                left,
                top,
                right,
                bottom,
                width: right - left,
                height: bottom - top,
            };
        }, {
            left: Number.POSITIVE_INFINITY,
            top: Number.POSITIVE_INFINITY,
            right: Number.NEGATIVE_INFINITY,
            bottom: Number.NEGATIVE_INFINITY,
            width: 0,
            height: 0,
        });
    }, [selectedElements]);
    const selectedLayerType = selectedElement ? getLayerType(selectedElement.id, activeScene) : null;

    const fitCanvas = () => {
        setInlineEditor(null);
        setManualViewport(false);
        setViewport((current) => {
            const next = normalizeViewport(
                createViewportToFocusBounds(viewportRect.width, stageHeight, activeScene.card),
                viewportRect.width,
                stageHeight,
                { mode: 'strict' },
            );
            if (
                Math.abs(current.scale - next.scale) < 0.0001 &&
                Math.abs(current.x - next.x) < 0.5 &&
                Math.abs(current.y - next.y) < 0.5
            ) {
                return current;
            }
            return next;
        });
    };

    const setOneHundredPercent = () => {
        setInlineEditor(null);
        setManualViewport(false);
        setViewport(
            normalizeViewport(
                {
                    scale: 1,
                    x: viewportRect.width / 2 - (activeScene.card.x + activeScene.card.width / 2),
                    y: stageHeight / 2 - (activeScene.card.y + activeScene.card.height / 2),
                },
                viewportRect.width,
                stageHeight,
                { mode: 'strict' },
            ),
        );
    };

    useEffect(() => {
        if (!containerRef.current || typeof ResizeObserver === 'undefined') {
            return undefined;
        }

        const observer = new ResizeObserver((entries) => {
            const entry = entries[0];
            const nextWidth = entry?.contentRect?.width || DEFAULT_VIEWPORT.width;

            setViewportRect({
                width: nextWidth,
                height: stageHeight,
            });
        });

        observer.observe(containerRef.current);
        return () => observer.disconnect();
    }, [stageHeight]);

    useEffect(() => {
        if (!stageShellRef.current || typeof ResizeObserver === 'undefined' || compact) {
            return undefined;
        }

        const observer = new ResizeObserver((entries) => {
            const entry = entries[0];
            const nextHeight = entry?.contentRect?.height || DEFAULT_VIEWPORT.height;
            setMeasuredStageHeight((current) => {
                const rounded = Math.round(nextHeight);
                return Math.abs(current - rounded) < 1 ? current : rounded;
            });
        });

        observer.observe(stageShellRef.current);
        return () => observer.disconnect();
    }, [compact]);

    useEffect(() => {
        setManualViewport(false);
        setViewport(
            normalizeViewport(
                createViewportToFocusBounds(viewportRect.width, stageHeight, activeScene.card),
                viewportRect.width,
                stageHeight,
                { mode: 'strict' },
            ),
        );
    }, [viewMode, layout.scene?.mode, activeScene.card?.x, activeScene.card?.y, activeScene.card?.width, activeScene.card?.height]);

    useEffect(() => {
        setViewport((current) => (
            manualViewport
                ? normalizeViewport(current, viewportRect.width, stageHeight, { mode: 'manual' })
                : normalizeViewport(
                    createViewportToFocusBounds(viewportRect.width, stageHeight, activeScene.card),
                    viewportRect.width,
                    stageHeight,
                    { mode: 'strict' },
                )
        ));
    }, [activeScene.card, manualViewport, stageHeight, viewportRect.width]);

    useEffect(() => {
        setInlineEditor(null);
    }, [selectedLayer, viewMode]);

    useEffect(() => {
        const activeSelection = (selectedLayers.length ? selectedLayers : [selectedLayer]).filter(Boolean);
        const selectedNodes = activeSelection.map((id) => nodeRefs.current[id]).filter(Boolean);
        if (!transformerRef.current) {
            return;
        }

        transformerRef.current.nodes(selectedNodes);
        transformerRef.current.getLayer()?.batchDraw();
    }, [selectedLayer, selectedLayers, renderableElements, viewMode]);

    useEffect(() => {
        if (!interactive) {
            return undefined;
        }

        const onKeyDown = (event) => {
            const target = event.target;
            const isTypingTarget = ['INPUT', 'TEXTAREA', 'SELECT'].includes(target?.tagName) || target?.isContentEditable;
            if (isTypingTarget) {
                return;
            }

            if (event.code === 'Space') {
                event.preventDefault();
                setSpacePressed(true);
                setInlineEditor(null);
                return;
            }

            if ((event.metaKey || event.ctrlKey) && event.key.toLowerCase() === '0') {
                event.preventDefault();
                fitCanvas();
                return;
            }

            if ((event.metaKey || event.ctrlKey) && event.key.toLowerCase() === '1') {
                event.preventDefault();
                setOneHundredPercent();
                return;
            }

            if (selectedLayer && (event.metaKey || event.ctrlKey) && event.key.toLowerCase() === 'd') {
                event.preventDefault();
                onDuplicateLayer?.();
                return;
            }

            if (selectedLayer && event.key.toLowerCase() === 'h') {
                event.preventDefault();
                onToggleLayerVisibility?.(selectedLayer);
                return;
            }

            if (selectedLayer && (event.key === 'Delete' || event.key === 'Backspace')) {
                event.preventDefault();
                onDeleteLayer?.();
                return;
            }

            if (!selectedElement) {
                return;
            }

            const step = event.shiftKey ? 10 : 1;
            if (event.key === 'ArrowLeft') {
                event.preventDefault();
                onLayoutElementChange(selectedLayer, { x: Math.round((selectedElement.x || 0) - step) });
            } else if (event.key === 'ArrowRight') {
                event.preventDefault();
                onLayoutElementChange(selectedLayer, { x: Math.round((selectedElement.x || 0) + step) });
            } else if (event.key === 'ArrowUp') {
                event.preventDefault();
                onLayoutElementChange(selectedLayer, { y: Math.round((selectedElement.y || 0) - step) });
            } else if (event.key === 'ArrowDown') {
                event.preventDefault();
                onLayoutElementChange(selectedLayer, { y: Math.round((selectedElement.y || 0) + step) });
            }
        };

        const onKeyUp = (event) => {
            if (event.code === 'Space') {
                setSpacePressed(false);
            }
        };

        window.addEventListener('keydown', onKeyDown);
        window.addEventListener('keyup', onKeyUp);
        return () => {
            window.removeEventListener('keydown', onKeyDown);
            window.removeEventListener('keyup', onKeyUp);
        };
    }, [fitCanvas, interactive, onDeleteLayer, onDuplicateLayer, onLayoutElementChange, onToggleLayerVisibility, selectedElement, selectedLayer, stageHeight, viewportRect.width]);

    useEffect(() => () => {
        if (rafGuidesRef.current) {
            window.cancelAnimationFrame(rafGuidesRef.current);
        }
    }, []);

    const scheduleGuides = (nextGuides) => {
        if (rafGuidesRef.current) {
            window.cancelAnimationFrame(rafGuidesRef.current);
        }

        rafGuidesRef.current = window.requestAnimationFrame(() => {
            setGuides(nextGuides);
            rafGuidesRef.current = null;
        });
    };

    const clearGuides = () => scheduleGuides([]);

    const stageBackground = useMemo(() => {
        if (viewMode === 'detail') {
            return (
                <>
                    <Rect x={0} y={0} width={CARD_STUDIO_CANVAS.width} height={CARD_STUDIO_CANVAS.height} fill="#EEF4FB" />
                    <Rect x={0} y={0} width={CARD_STUDIO_CANVAS.width} height={CARD_STUDIO_CANVAS.height} fill="rgba(11,33,53,0.18)" />
                </>
            );
        }

        if (layout.scene.mode === 'program_news') {
            return (
                <>
                    <Rect x={34} y={36} width={1212} height={708} cornerRadius={48} fill="#F4F4FA" />
                    <Rect
                        x={34}
                        y={76}
                        width={1212}
                        height={632}
                        cornerRadius={44}
                        fillLinearGradientStartPoint={{ x: 0, y: 0 }}
                        fillLinearGradientEndPoint={{ x: 1212, y: 632 }}
                        fillLinearGradientColorStops={[0, '#65CCE9', 0.52, '#EEF3EC', 1, '#F5D65D']}
                    />
                    <Rect x={34} y={76} width={1212} height={632} cornerRadius={44} fill="rgba(255,255,255,0.18)" />
                    <Rect x={36} y={420} width={1210} height={286} fill="rgba(255,255,255,0.55)" />
                </>
            );
        }

        return (
            <>
                <Rect x={22} y={30} width={1236} height={742} cornerRadius={40} fill="#F4F4FA" />
                <Rect
                    x={22}
                    y={36}
                    width={1236}
                    height={360}
                    cornerRadius={40}
                    fillLinearGradientStartPoint={{ x: 0, y: 0 }}
                    fillLinearGradientEndPoint={{ x: 1236, y: 320 }}
                    fillLinearGradientColorStops={[0, '#38B7D1', 0.56, '#C7E36E', 1, '#F4DF19']}
                    opacity={0.96}
                />
                <Rect
                    x={22}
                    y={290}
                    width={1236}
                    height={112}
                    fillLinearGradientStartPoint={{ x: 0, y: 0 }}
                    fillLinearGradientEndPoint={{ x: 1236, y: 0 }}
                    fillLinearGradientColorStops={[0, '#FFFFFF', 0.55, '#F7F9FF', 1, '#FFFFFF']}
                    opacity={0.86}
                />
                <Rect x={72} y={510} width={420} height={200} cornerRadius={32} fill="rgba(255,255,255,0.72)" />
                <Rect x={526} y={510} width={420} height={200} cornerRadius={32} fill="rgba(255,255,255,0.66)" />
                <Rect x={980} y={510} width={220} height={200} cornerRadius={32} fill="rgba(255,255,255,0.56)" />
            </>
        );
    }, [layout.scene.mode, viewMode]);

    const getLayerDisplayText = (layerId) => {
        switch (getLayerType(layerId, activeScene)) {
            case 'badge':
                return card.badgeText || 'Badge';
            case 'title':
                return viewMode === 'detail' ? detailTitleText : titleText;
            case 'summary':
                return summaryText;
            case 'body':
                return detailBodyText;
            case 'date':
                return dateText;
            case 'button':
                return buttonText;
            case 'freeText': {
                const custom = activeScene.customElements?.find((item) => item.id === layerId);
                return custom?.text || '';
            }
            default:
                return '';
        }
    };

    const startInlineEditing = (layerId) => {
        if (!interactive || !INLINE_TEXT_LAYER_KEYS.has(getLayerType(layerId, activeScene))) {
            return;
        }

        setInlineEditor({
            id: layerId,
            value: getLayerDisplayText(layerId),
            multiline: ['title', 'summary', 'body'].includes(getLayerType(layerId, activeScene)),
        });
    };

    const commitInlineEditor = () => {
        if (!inlineEditor) {
            return;
        }

        onInlineTextChange?.(inlineEditor.id, inlineEditor.value);
        setInlineEditor(null);
    };

    const renderNodeContent = (item) => {
        switch (item.sourceKey) {
            case 'image':
                if (!item.visible || !cardImage) {
                    return (
                        <>
                            <Rect width={item.width} height={item.height} cornerRadius={viewMode === 'detail' ? 24 : 28} fill="#EEF7FA" opacity={0.32} />
                            <Text
                                x={20}
                                y={(item.height / 2) - 12}
                                width={Math.max(item.width - 40, 0)}
                                align="center"
                                text="Upload an image"
                                fontFamily="Poppins"
                                fontSize={18}
                                fontStyle="bold"
                                fill="#69839B"
                            />
                        </>
                    );
                }
                const imageTransform = getImageDrawTransform(item);
                return (
                    <Group clipFunc={(ctx) => applyRoundedClip(ctx, item.width, item.height, item.radius ?? (viewMode === 'detail' ? 24 : 28))}>
                        <Rect width={item.width} height={item.height} fill="#EEF7FA" opacity={0.24} />
                        <KonvaImage
                            image={cardImage}
                            x={imageTransform.x}
                            y={imageTransform.y}
                            width={imageTransform.width}
                            height={imageTransform.height}
                            rotation={imageTransform.rotation}
                            opacity={item.opacity ?? (viewMode === 'detail' ? 1 : 0.22)}
                            {...buildImageCrop(item, cardImage)}
                        />
                    </Group>
                );
            case 'logo':
                return (
                    <>
                        <Rect
                            width={item.width}
                            height={item.height}
                            cornerRadius={28}
                            fillLinearGradientStartPoint={{ x: 0, y: 0 }}
                            fillLinearGradientEndPoint={{ x: item.width, y: item.height }}
                            fillLinearGradientColorStops={[0, getItemBackgroundColor(item, '#D9F4FA'), 1, item.secondaryFill || '#FFF5C8']}
                        />
                        <Rect
                            x={18}
                            y={18}
                            width={Math.max(item.width - 36, 52)}
                            height={Math.max(item.height - 36, 52)}
                            cornerRadius={20}
                            fill={item.innerFill || '#FFFFFF'}
                            shadowColor="#0B2135"
                            shadowBlur={20}
                            shadowOpacity={0.08}
                            shadowOffsetY={6}
                        />
                        {logoImage ? (
                            <KonvaImage image={logoImage} x={30} y={30} width={Math.max(item.width - 60, 36)} height={Math.max(item.height - 60, 36)} />
                        ) : (
                            <Text x={30} y={(item.height / 2) - 8} text="No logo" fontSize={18} fill="#5D6C7D" fontStyle="bold" />
                        )}
                    </>
                );
            case 'badge':
                return (
                    <>
                        <Rect width={item.width} height={item.height} cornerRadius={item.radius || 999} fill={getItemBackgroundColor(item, badgeColors.fill)} />
                        <Text x={18} y={8} width={item.width - 36} text={card.badgeText || 'Badge'} fontFamily={item.fontFamily || 'Poppins'} fontStyle="bold" fontSize={item.fontSize} fill={getItemTextColor(item, badgeColors.text)} ellipsis />
                    </>
                );
            case 'title':
                return (
                    <>
                        <Rect width={item.width} height={item.height} fill="rgba(0,0,0,0.001)" />
                        <Text width={item.width} text={viewMode === 'detail' ? detailTitleText : titleText} fontFamily={item.fontFamily || 'Poppins'} fontStyle="bold" fontSize={item.fontSize} lineHeight={item.lineHeight || 1.08} fill={getItemTextColor(item, '#0B2135')} />
                    </>
                );
            case 'summary':
                return (
                    <>
                        <Rect width={item.width} height={item.height} fill="rgba(0,0,0,0.001)" />
                        <Text width={item.width} text={summaryText} fontFamily={item.fontFamily || 'Poppins'} fontStyle="normal" fontSize={item.fontSize} lineHeight={item.lineHeight || 1.45} fill={getItemTextColor(item, '#4F6277')} />
                    </>
                );
            case 'body':
                return (
                    <>
                        <Rect width={item.width} height={item.height} fill="rgba(0,0,0,0.001)" />
                        <Text width={item.width} text={detailBodyText} fontFamily={item.fontFamily || 'Poppins'} fontStyle="normal" fontSize={item.fontSize} lineHeight={item.lineHeight || 1.45} fill={getItemTextColor(item, '#4F6277')} />
                    </>
                );
            case 'date':
                return (
                    <>
                        <Rect width={item.width} height={item.height} fill="rgba(0,0,0,0.001)" />
                        <Text width={item.width} text={dateText} fontFamily={item.fontFamily || 'Poppins'} fontStyle="normal" fontSize={item.fontSize} fill={getItemTextColor(item, '#8F9FB2')} />
                    </>
                );
            case 'button':
                return (
                    <>
                        <Rect width={item.width} height={item.height} cornerRadius={item.radius || 999} fill={getItemBackgroundColor(item, '#E67B32')} shadowColor={getItemBackgroundColor(item, '#E67B32')} shadowBlur={20} shadowOpacity={0.22} shadowOffsetY={8} />
                        <Text
                            x={28}
                            y={(item.height - item.fontSize) / 2 - 2}
                            width={item.width - 56}
                            text={`${buttonText} ↗`}
                            fontFamily={item.fontFamily || 'Poppins'}
                            fontStyle="bold"
                            fontSize={item.fontSize}
                            fill={getItemTextColor(item, '#FFFFFF')}
                            align="center"
                        />
                    </>
                );
            case 'freeText':
                return (
                    <>
                        <Rect width={item.width} height={item.height} fill="rgba(0,0,0,0.001)" />
                        <Text
                            width={item.width}
                            height={item.height}
                            text={item.text || 'Text box'}
                            fontFamily={item.fontFamily || 'Poppins'}
                            fontStyle={item.fontStyle || 'normal'}
                            fontSize={item.fontSize || 20}
                            lineHeight={item.lineHeight || 1.3}
                            fill={getItemTextColor(item, '#0B2135')}
                            ellipsis
                        />
                    </>
                );
            default:
                return null;
        }
    };

    const gridLines = useMemo(() => {
        const vertical = [];
        const horizontal = [];

        for (let x = 0; x <= CARD_STUDIO_CANVAS.width; x += GRID_SIZE) {
            vertical.push([x, 0, x, CARD_STUDIO_CANVAS.height]);
        }

        for (let y = 0; y <= CARD_STUDIO_CANVAS.height; y += GRID_SIZE) {
            horizontal.push([0, y, CARD_STUDIO_CANVAS.width, y]);
        }

        return { vertical, horizontal };
    }, []);

    const getSnappedPosition = (element, position) => {
        const width = element?.width || 0;
        const height = element?.height || 0;
        const tolerance = 10;
        const guideLines = [];
        let nextX = clamp(position.x, 0, CARD_STUDIO_CANVAS.width - width);
        let nextY = clamp(position.y, 0, CARD_STUDIO_CANVAS.height - height);

        const dynamicGuides = renderableElements
            .filter((item) => item.id !== element?.id && item.visible !== false)
            .flatMap((item) => {
                const bounds = getElementBounds(item);
                return [
                    { orientation: 'vertical', value: bounds.x },
                    { orientation: 'vertical', value: bounds.x + bounds.width / 2 },
                    { orientation: 'vertical', value: bounds.x + bounds.width },
                    { orientation: 'horizontal', value: bounds.y },
                    { orientation: 'horizontal', value: bounds.y + bounds.height / 2 },
                    { orientation: 'horizontal', value: bounds.y + bounds.height },
                ];
            });

        const verticalGuides = [
            activeScene.card.x,
            activeScene.card.x + activeScene.card.width / 2,
            activeScene.card.x + activeScene.card.width,
            CARD_STUDIO_CANVAS.width / 2,
            ...dynamicGuides.filter((item) => item.orientation === 'vertical').map((item) => item.value),
        ];
        const horizontalGuides = [
            activeScene.card.y,
            activeScene.card.y + activeScene.card.height / 2,
            activeScene.card.y + activeScene.card.height,
            CARD_STUDIO_CANVAS.height / 2,
            ...dynamicGuides.filter((item) => item.orientation === 'horizontal').map((item) => item.value),
        ];

        const snapAxis = (guidesPool, edges, stageLength, orientation) => {
            let best = null;
            guidesPool.forEach((guide) => {
                edges.forEach((edge) => {
                    const distance = Math.abs(edge.value - guide);
                    if (distance <= tolerance && (!best || distance < best.distance)) {
                        best = { guide, edge, distance };
                    }
                });
            });

            if (!best) {
                return null;
            }

            guideLines.push(
                orientation === 'vertical'
                    ? { points: [best.guide, 0, best.guide, stageLength] }
                    : { points: [0, best.guide, stageLength, best.guide] },
            );

            return best.guide - best.edge.offset;
        };

        const snappedX = snapAxis(
            verticalGuides,
            [
                { value: nextX, offset: 0 },
                { value: nextX + width / 2, offset: width / 2 },
                { value: nextX + width, offset: width },
            ],
            CARD_STUDIO_CANVAS.height,
            'vertical',
        );
        const snappedY = snapAxis(
            horizontalGuides,
            [
                { value: nextY, offset: 0 },
                { value: nextY + height / 2, offset: height / 2 },
                { value: nextY + height, offset: height },
            ],
            CARD_STUDIO_CANVAS.width,
            'horizontal',
        );

        if (typeof snappedX === 'number') {
            nextX = snappedX;
        }
        if (typeof snappedY === 'number') {
            nextY = snappedY;
        }

        return { x: nextX, y: nextY, guides: guideLines };
    };

    const handleNodeDragMove = (elementKey, event, element) => {
        const { x, y, guides: nextGuides } = getSnappedPosition({ ...element, id: elementKey }, {
            x: event.target.x(),
            y: event.target.y(),
        });

        event.target.position({ x, y });
        scheduleGuides(nextGuides);
    };

    const zoomAtPoint = (pointerPosition, nextScale) => {
        const clampedScale = clamp(nextScale, 0.24, 2.4);
        const pointerX = pointerPosition?.x ?? viewportRect.width / 2;
        const pointerY = pointerPosition?.y ?? stageHeight / 2;
        const worldX = (pointerX - viewport.x) / viewport.scale;
        const worldY = (pointerY - viewport.y) / viewport.scale;

        const nextViewport = normalizeViewport(
            {
                scale: clampedScale,
                x: pointerX - (worldX * clampedScale),
                y: pointerY - (worldY * clampedScale),
            },
            viewportRect.width,
            stageHeight,
            { mode: 'manual' },
        );

        setManualViewport(true);
        setViewport(nextViewport);
    };

    const handleWheel = (event) => {
        if (!interactive) {
            return;
        }

        event.evt.preventDefault();
        setInlineEditor(null);
        const stage = stageRef.current;
        const pointer = stage?.getPointerPosition();
        const direction = event.evt.deltaY > 0 ? -1 : 1;
        const zoomFactor = direction > 0 ? 1.08 : 0.92;

        zoomAtPoint(pointer, viewport.scale * zoomFactor);
    };

    const currentCursor = !interactive
        ? 'default'
        : (interactionMode === 'pan' || spacePressed ? (isPanning ? 'grabbing' : 'grab') : selectedLayer ? 'default' : 'crosshair');
    const inlineTarget = inlineEditor ? renderableElements.find((item) => item.id === inlineEditor.id) : null;
    const inlineOverlayStyle = inlineTarget
        ? {
              left: `${viewport.x + (inlineTarget.x * viewport.scale)}px`,
              top: `${viewport.y + (inlineTarget.y * viewport.scale)}px`,
              width: `${Math.max(120, inlineTarget.width * viewport.scale)}px`,
              minHeight: `${Math.max(42, inlineTarget.height * viewport.scale)}px`,
              fontSize: `${Math.max(12, (inlineTarget.fontSize || 16) * viewport.scale)}px`,
              lineHeight: inlineTarget.lineHeight || 1.2,
              fontFamily: inlineTarget.fontFamily || 'Poppins',
              color: getItemTextColor(inlineTarget, TYPOGRAPHY_FALLBACKS[getLayerType(inlineEditor.id, activeScene)] || '#0B2135'),
          }
        : null;
    const selectionToolbarStyle = selectedBounds
        ? (() => {
            const centerX = viewport.x + ((selectedBounds.left + (selectedBounds.width / 2)) * viewport.scale);
            const topY = viewport.y + (selectedBounds.top * viewport.scale);
            const placeBelow = topY < 96;

            return {
                left: `${centerX}px`,
                top: `${Math.max(18, placeBelow ? topY + (selectedBounds.height * viewport.scale) + 20 : topY - 16)}px`,
                transform: `translate(-50%, ${placeBelow ? '0' : '-100%'})`,
            };
        })()
        : null;
    const cropPadStyle = selectedBounds && selectedLayerType === 'image'
        ? {
            left: `${viewport.x + ((selectedBounds.right + 18) * viewport.scale)}px`,
            top: `${viewport.y + ((selectedBounds.top + (selectedBounds.height / 2)) * viewport.scale)}px`,
            transform: 'translateY(-50%)',
        }
        : null;

    const startManualPan = () => {
        const stage = stageRef.current;
        const pointer = stage?.getPointerPosition();

        if (!pointer) {
            return false;
        }

        panSessionRef.current = {
            pointer,
            viewport: { ...viewport },
        };
        setIsPanning(true);
        setInlineEditor(null);
        clearGuides();
        return true;
    };

    const updateManualPan = () => {
        const stage = stageRef.current;
        const pointer = stage?.getPointerPosition();
        const session = panSessionRef.current;

        if (!pointer || !session) {
            return;
        }

        setManualViewport(true);
        setViewport({
            ...session.viewport,
            x: session.viewport.x + (pointer.x - session.pointer.x),
            y: session.viewport.y + (pointer.y - session.pointer.y),
        });
    };

    const endManualPan = () => {
        panSessionRef.current = null;
        setIsPanning(false);
    };

    return (
        <div className={`card-studio-canvas ${compact ? 'is-compact' : ''}`} ref={containerRef}>
            <div ref={stageShellRef} className="card-studio-canvas__stageShell">
                {interactive ? (
                    <div className="card-studio-canvas__toolbar">
                        <div className="card-studio-canvas__toolbarGroup">
                            <span className="card-studio-canvas__toolbarLabel">Mode</span>
                            <CanvasToolbarButton active={interactionMode === 'select'} onClick={() => setInteractionMode('select')}>
                                Select
                            </CanvasToolbarButton>
                            <CanvasToolbarButton active={interactionMode === 'pan'} onClick={() => setInteractionMode((current) => (current === 'pan' ? 'select' : 'pan'))}>
                                Pan
                            </CanvasToolbarButton>
                        </div>
                        <div className="card-studio-canvas__toolbarGroup">
                            <CanvasToolbarButton onClick={() => zoomAtPoint(null, viewport.scale / 1.12)}>-</CanvasToolbarButton>
                            <span className="card-studio-canvas__zoom">{Math.round(viewport.scale * 100)}%</span>
                            <CanvasToolbarButton onClick={() => zoomAtPoint(null, viewport.scale * 1.12)}>+</CanvasToolbarButton>
                            <CanvasToolbarButton onClick={setOneHundredPercent}>100%</CanvasToolbarButton>
                            <CanvasToolbarButton onClick={fitCanvas}>Fit</CanvasToolbarButton>
                        </div>
                    </div>
                ) : (
                    <div className="card-studio-canvas__readonlyPill">Read-only preview</div>
                )}
                {interactive && activeSelection.length ? (
                    <button
                        type="button"
                        className={`card-studio-canvas__selectionToggle ${selectionHudVisible ? 'is-active' : ''}`}
                        onClick={() => setSelectionHudVisible((current) => !current)}
                    >
                        {selectionHudVisible ? 'Hide layer tools' : 'Show layer tools'}
                    </button>
                ) : null}
                {interactive && selectionHudVisible && activeSelection.length && selectionToolbarStyle ? (
                    <div className="card-studio-canvas__selectionToolbar" style={selectionToolbarStyle}>
                        <div className="card-studio-canvas__selectionToolbarMeta">
                            <strong>{activeSelection.length > 1 ? `${activeSelection.length} selected` : selectedElement?.sourceKey || 'Layer'}</strong>
                            <span>{selectedLayerType === 'image' ? 'Image tools' : 'Layer tools'}</span>
                        </div>
                        <div className="card-studio-canvas__selectionToolbarActions">
                            <CanvasToolbarIconButton title="Duplicate selection" disabled={!selectedLayer} onClick={() => onDuplicateLayer?.()}>
                                <FiCopy />
                            </CanvasToolbarIconButton>
                            <CanvasToolbarIconButton title={selectedElement?.visible === false ? 'Show selection' : 'Hide selection'} disabled={!selectedLayer} onClick={() => onToggleLayerVisibility?.(selectedLayer)}>
                                {selectedElement?.visible === false ? <FiEye /> : <FiEyeOff />}
                            </CanvasToolbarIconButton>
                            <CanvasToolbarIconButton title="Bring to front" disabled={!activeSelection.length} onClick={() => onArrangeSelection?.('front')}>
                                <FiLayers />
                            </CanvasToolbarIconButton>
                            <CanvasToolbarIconButton title="Send to back" disabled={!activeSelection.length} onClick={() => onArrangeSelection?.('back')}>
                                <FiCornerUpLeft />
                            </CanvasToolbarIconButton>
                            <CanvasToolbarIconButton title="Align horizontal center" disabled={!activeSelection.length} onClick={() => onAlignSelection?.('center')}>
                                <FiArrowLeft />
                            </CanvasToolbarIconButton>
                            <CanvasToolbarIconButton title="Align vertical middle" disabled={!activeSelection.length} onClick={() => onAlignSelection?.('middle')}>
                                <FiArrowUp />
                            </CanvasToolbarIconButton>
                            {selectedLayerType === 'image' ? (
                                <>
                                    <CanvasToolbarIconButton title="Fill frame" onClick={() => onApplyImageFramePreset?.('fill')}>
                                        <FiMaximize2 />
                                    </CanvasToolbarIconButton>
                                    <CanvasToolbarIconButton title="Contain image" onClick={() => onApplyImageFramePreset?.('contain')}>
                                        <FiMinimize2 />
                                    </CanvasToolbarIconButton>
                                </>
                            ) : null}
                            <CanvasToolbarIconButton title="Delete selection" disabled={!selectedLayer} onClick={() => onDeleteLayer?.()}>
                                <FiTrash2 />
                            </CanvasToolbarIconButton>
                        </div>
                    </div>
                ) : null}
                {interactive && selectionHudVisible && cropPadStyle ? (
                    <div className="card-studio-canvas__cropPad" style={cropPadStyle}>
                        <div className="card-studio-canvas__cropPadLabel">Crop</div>
                        <div className="card-studio-canvas__cropPadGrid">
                            <CanvasToolbarIconButton title="Crop up" onClick={() => onNudgeImageCrop?.(0, -12, 0)}>
                                <FiArrowUp />
                            </CanvasToolbarIconButton>
                            <CanvasToolbarIconButton title="Crop left" onClick={() => onNudgeImageCrop?.(-12, 0, 0)}>
                                <FiArrowLeft />
                            </CanvasToolbarIconButton>
                            <CanvasToolbarIconButton title="Crop right" onClick={() => onNudgeImageCrop?.(12, 0, 0)}>
                                <FiArrowRight />
                            </CanvasToolbarIconButton>
                            <CanvasToolbarIconButton title="Crop down" onClick={() => onNudgeImageCrop?.(0, 12, 0)}>
                                <FiArrowDown />
                            </CanvasToolbarIconButton>
                        </div>
                        <div className="card-studio-canvas__cropPadActions">
                            <CanvasToolbarIconButton title="Zoom crop" onClick={() => onNudgeImageCrop?.(0, 0, 18)}>
                                <FiMaximize2 />
                            </CanvasToolbarIconButton>
                            <CanvasToolbarIconButton title="Reset crop" onClick={() => onApplyImageFramePreset?.('reset')}>
                                <FiMinimize2 />
                            </CanvasToolbarIconButton>
                        </div>
                    </div>
                ) : null}
                {inlineTarget && inlineOverlayStyle ? (
                    <div className="card-studio-canvas__inlineEditor" style={inlineOverlayStyle}>
                        {inlineEditor.multiline ? (
                            <textarea
                                autoFocus
                                value={inlineEditor.value}
                                onChange={(event) => setInlineEditor((current) => ({ ...current, value: event.target.value }))}
                                onBlur={commitInlineEditor}
                                onKeyDown={(event) => {
                                    if ((event.metaKey || event.ctrlKey) && event.key === 'Enter') {
                                        event.preventDefault();
                                        commitInlineEditor();
                                    }
                                    if (event.key === 'Escape') {
                                        event.preventDefault();
                                        setInlineEditor(null);
                                    }
                                }}
                            />
                        ) : (
                            <input
                                autoFocus
                                value={inlineEditor.value}
                                onChange={(event) => setInlineEditor((current) => ({ ...current, value: event.target.value }))}
                                onBlur={commitInlineEditor}
                                onKeyDown={(event) => {
                                    if (event.key === 'Enter') {
                                        event.preventDefault();
                                        commitInlineEditor();
                                    }
                                    if (event.key === 'Escape') {
                                        event.preventDefault();
                                        setInlineEditor(null);
                                    }
                                }}
                            />
                        )}
                    </div>
                ) : null}
                {dropActive ? <div className="card-studio-canvas__dropHint">Drop an image to update the card asset</div> : null}
                <Stage
                    ref={stageRef}
                    width={viewportRect.width}
                    height={stageHeight}
                    style={{ cursor: currentCursor }}
                    onDragOver={(event) => {
                        event.evt.preventDefault();
                        if (event.evt.dataTransfer?.types?.includes('Files')) {
                            setDropActive((current) => (current ? current : true));
                        }
                    }}
                    onDragLeave={() => setDropActive(false)}
                    onDrop={(event) => {
                        event.evt.preventDefault();
                        setDropActive(false);
                        onAssetDrop?.(event.evt);
                    }}
                    onWheel={handleWheel}
                    onMouseDown={(event) => {
                        if (!interactive) {
                            return;
                        }

                        if (interactionMode === 'pan' || spacePressed) {
                            startManualPan();
                            return;
                        }

                        const clickedOnEmpty = event.target === event.target.getStage();
                        if (clickedOnEmpty) {
                            onSelectLayer(null);
                            setInlineEditor(null);
                            clearGuides();
                        }
                    }}
                    onMouseMove={() => {
                        if (!interactive || !panSessionRef.current) {
                            return;
                        }

                        updateManualPan();
                    }}
                    onMouseUp={endManualPan}
                    onMouseLeave={endManualPan}
                >
                    <Layer>
                        <Rect
                            x={0}
                            y={0}
                            width={viewportRect.width}
                            height={stageHeight}
                            fill="#F6F9FC"
                        />

                        <Group x={viewport.x} y={viewport.y} scaleX={viewport.scale} scaleY={viewport.scale}>
                            <Rect x={0} y={0} width={CARD_STUDIO_CANVAS.width} height={CARD_STUDIO_CANVAS.height} cornerRadius={30} fill="#F2F6FB" shadowColor="#0B2135" shadowBlur={42} shadowOpacity={0.06} />

                            {gridLines.vertical.map((points) => (
                                <Line key={`v-${points[0]}`} points={points} stroke="rgba(11,33,53,0.06)" strokeWidth={1} listening={false} />
                            ))}
                            {gridLines.horizontal.map((points) => (
                                <Line key={`h-${points[1]}`} points={points} stroke="rgba(11,33,53,0.05)" strokeWidth={1} listening={false} />
                            ))}

                            {stageBackground}

                            <Rect
                                x={activeScene.card.x}
                                y={activeScene.card.y}
                                width={activeScene.card.width}
                                height={activeScene.card.height}
                                cornerRadius={activeScene.card.radius}
                                fill={activeScene.card.gradientStops ? undefined : cardFill}
                                fillLinearGradientStartPoint={activeScene.card.gradientStops ? { x: activeScene.card.x, y: activeScene.card.y } : undefined}
                                fillLinearGradientEndPoint={activeScene.card.gradientStops ? { x: activeScene.card.x + activeScene.card.width, y: activeScene.card.y + activeScene.card.height } : undefined}
                                fillLinearGradientColorStops={activeScene.card.gradientStops || undefined}
                                stroke={cardStroke}
                                strokeWidth={1}
                                shadowColor={cardShadowColor}
                                shadowBlur={cardShadowBlur}
                                shadowOpacity={cardShadowOpacity}
                                shadowOffsetY={cardShadowOffsetY}
                            />

                            <Rect
                                x={activeScene.card.x - 16}
                                y={activeScene.card.y - 16}
                                width={activeScene.card.width + 32}
                                height={activeScene.card.height + 32}
                                cornerRadius={activeScene.card.radius + 14}
                                stroke="rgba(53,185,209,0.18)"
                                strokeWidth={2}
                                dash={[10, 8]}
                                listening={false}
                            />

                            {renderableElements.map((item) => (
                                <CanvasNode
                                    key={item.id}
                                    elementKey={item.id}
                                    selected={activeSelection.includes(item.id)}
                                    isTransformable={item.sourceKey !== 'date' && item.sourceKey !== 'body'}
                                    element={item}
                                    refs={nodeRefs}
                                    onSelect={onSelectLayer}
                                    onChange={onLayoutElementChange}
                                    onDragMove={handleNodeDragMove}
                                    onDragEnd={clearGuides}
                                    onStartInlineEdit={startInlineEditing}
                                    interactive={interactive && interactionMode !== 'pan' && !spacePressed}
                                >
                                    {renderNodeContent(item)}
                                </CanvasNode>
                            ))}

                            {guides.map((guide, index) => (
                                <Group key={`${guide.points.join('-')}-${index}`} listening={false}>
                                    <Line
                                        points={guide.points}
                                        stroke="#35B9D1"
                                        strokeWidth={2}
                                        dash={[10, 8]}
                                        opacity={0.78}
                                        listening={false}
                                    />
                                </Group>
                            ))}

                            {interactive ? (
                                <Transformer
                                    ref={transformerRef}
                                    rotateEnabled={getLayerType(selectedLayer, activeScene) !== 'date'}
                                    rotationSnaps={[0, 45, 90, 135, 180, 225, 270, 315]}
                                    anchorSize={10}
                                    borderDash={[6, 4]}
                                    borderStroke="#35B9D1"
                                    anchorStroke="#35B9D1"
                                    anchorFill="#FFFFFF"
                                    enabledAnchors={
                                        ['logo', 'image'].includes(getLayerType(selectedLayer, activeScene))
                                            ? ['top-left', 'top-center', 'top-right', 'middle-left', 'middle-right', 'bottom-left', 'bottom-center', 'bottom-right']
                                            : ['middle-left', 'middle-right']
                                    }
                                    boundBoxFunc={(oldBox, newBox) => {
                                        if (newBox.width < MIN_SIZE || newBox.height < MIN_SIZE) {
                                            return oldBox;
                                        }
                                        return newBox;
                                    }}
                                />
                            ) : null}
                        </Group>
                    </Layer>
                </Stage>
            </div>
            {interactive ? (
                <div className="card-studio-canvas__status">
                    <div>
                        <strong>{activeSelection.length > 1 ? `${activeSelection.length} layers selected` : selectedLayer ? `Editing ${selectedLayer}` : 'Canvas ready'}</strong>
                        <span>{selectedLayer ? 'Double-click text to edit inline. Shift-click to multi-select. Scroll to zoom.' : 'Pick a layer to start editing.'}</span>
                    </div>
                    {selectedElement ? (
                        <div className="card-studio-canvas__metrics">
                            <span>X {Math.round(selectedElement.x || 0)}</span>
                            <span>Y {Math.round(selectedElement.y || 0)}</span>
                            <span>W {Math.round(selectedElement.width || 0)}</span>
                            <span>H {Math.round(selectedElement.height || 0)}</span>
                            {selectedElement.rotation ? <span>R {Math.round(selectedElement.rotation)}°</span> : null}
                        </div>
                    ) : null}
                </div>
            ) : null}
        </div>
    );
}
