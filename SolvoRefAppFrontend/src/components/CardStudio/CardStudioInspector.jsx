import { Suspense, lazy, useEffect, useMemo, useState } from 'react';
import { FiArrowDown, FiArrowUp, FiClock, FiEye, FiEyeOff, FiImage, FiLayers, FiMenu, FiMousePointer, FiPlus, FiSettings, FiShield, FiType } from 'react-icons/fi';
import {
    ACTION_TYPES,
    BADGE_VARIANTS,
    CONTENT_ICON_MAP,
    CONTENT_SECTIONS,
    ICON_OPTIONS,
    getContentIcon,
} from '../../content/homeContentConfig';
import { createEditableDetailFromBuiltin } from '../../content/homePromoBuiltins';
import { resolveContentAssetUrl } from '../../config/api';

const PANEL_META = {
    text: { label: 'Text', icon: FiType },
    style: { label: 'Style', icon: FiSettings },
    layers: { label: 'Layers', icon: FiLayers },
    media: { label: 'Media', icon: FiImage },
    action: { label: 'Action', icon: FiMousePointer },
    publish: { label: 'Publish', icon: FiShield },
};

const FONT_OPTIONS = ['Poppins', 'DM Sans', 'Merriweather', 'Space Grotesk'];
const IMAGE_UPLOAD_ACCEPT = [
    'image/*',
    '.png',
    '.apng',
    '.jpg',
    '.jpeg',
    '.jpe',
    '.jfif',
    '.webp',
    '.gif',
    '.svg',
    '.avif',
    '.bmp',
    '.ico',
    '.tif',
    '.tiff',
    '.heic',
    '.heif',
].join(',');
const TYPOGRAPHY_PRESETS = [
    { label: 'Headline', fontFamily: 'Poppins', fontSize: 46, lineHeight: 1.06, fill: '#0B2135' },
    { label: 'Editorial', fontFamily: 'Merriweather', fontSize: 42, lineHeight: 1.12, fill: '#0E2237' },
    { label: 'Compact', fontFamily: 'DM Sans', fontSize: 38, lineHeight: 1.08, fill: '#102A43' },
];
const PALETTE_PRESETS = [
    {
        label: 'Citrus',
        surface: '#FFFFFF',
        surfaceStroke: '#E7EDF4',
        primary: '#0B2135',
        support: '#4F6277',
        badgeFill: '#F7EEC4',
        badgeText: '#B58A0F',
        buttonFill: '#E67B32',
        buttonText: '#FFFFFF',
        gradientStops: [0, '#FFFFFF', 0.52, '#FFF4C7', 1, '#FFE0C5'],
    },
    {
        label: 'Ocean',
        surface: '#F9FEFF',
        surfaceStroke: '#D3EDF2',
        primary: '#0C3D4B',
        support: '#47616E',
        badgeFill: '#D7F4FC',
        badgeText: '#147085',
        buttonFill: '#148EA4',
        buttonText: '#FFFFFF',
        gradientStops: [0, '#F9FEFF', 0.5, '#DFF7FC', 1, '#E8FAF6'],
    },
    {
        label: 'Midnight',
        surface: '#F9FAFD',
        surfaceStroke: '#D9E1EC',
        primary: '#14233A',
        support: '#62748A',
        badgeFill: '#E8EEF8',
        badgeText: '#324B72',
        buttonFill: '#1D3153',
        buttonText: '#FFFFFF',
        gradientStops: [0, '#F9FAFD', 0.46, '#EAF0FA', 1, '#DDE8F5'],
    },
];

const MODAL_OPTIONS = [
    { value: 'incentive', label: 'Referral incentive' },
    { value: 'success', label: 'Success stories' },
    { value: 'community', label: 'Community story' },
    { value: 'update', label: 'Program update' },
    { value: 'campaign', label: 'Campaign' },
];

const ReactQuill = lazy(() => import('react-quill-new'));

const quillModules = {
    toolbar: [
        [{ header: [1, 2, 3, false] }],
        [{ font: [] }],
        [{ size: ['small', false, 'large', 'huge'] }],
        ['bold', 'italic', 'underline', 'strike'],
        [{ color: [] }, { background: [] }],
        [{ align: [] }],
        [{ list: 'ordered' }, { list: 'bullet' }],
        ['blockquote', 'link', 'clean'],
    ],
};

const quillFormats = [
    'header',
    'font',
    'size',
    'bold',
    'italic',
    'underline',
    'strike',
    'color',
    'background',
    'align',
    'list',
    'bullet',
    'blockquote',
    'link',
];

function normalizeHex(value, fallback = '#0B2135') {
    if (!value) {
        return fallback;
    }

    const next = value.trim();
    if (/^#[0-9a-fA-F]{6}$/.test(next)) {
        return next;
    }

    return fallback;
}

function NumberField({ label, value, onChange, step = 1, min = 0, max }) {
    return (
        <label className="card-studio-inspector__field">
            <span>{label}</span>
            <input
                type="number"
                step={step}
                min={min}
                max={max}
                value={Number.isFinite(Number(value)) ? value : ''}
                onChange={(event) => onChange(Number(event.target.value) || 0)}
            />
        </label>
    );
}

function ColorField({ label, value, onChange, fallback }) {
    const normalized = normalizeHex(value, fallback);

    return (
        <div className="card-studio-inspector__field card-studio-inspector__field--color">
            <span>{label}</span>
            <div className="card-studio-inspector__colorRow">
                <input type="color" value={normalized} onChange={(event) => onChange(event.target.value)} />
                <input type="text" value={value || normalized} onChange={(event) => onChange(event.target.value)} placeholder={fallback} />
            </div>
        </div>
    );
}

function RangeField({ label, value, onChange, min = 0, max = 1, step = 0.05 }) {
    const normalized = Number.isFinite(Number(value)) ? Number(value) : min;

    return (
        <label className="card-studio-inspector__field">
            <span>{label}</span>
            <div className="card-studio-inspector__rangeRow">
                <input type="range" min={min} max={max} step={step} value={normalized} onChange={(event) => onChange(Number(event.target.value))} />
                <strong>{Math.round(normalized * 100)}%</strong>
            </div>
        </label>
    );
}

function RichTextEditor({ value, onChange, readOnly = false }) {
    return (
        <Suspense fallback={<div className="card-studio-inspector__editorFallback">Loading editor...</div>}>
            <ReactQuill theme="snow" value={value} onChange={onChange} modules={quillModules} formats={quillFormats} readOnly={readOnly} />
        </Suspense>
    );
}

function LayerQuickActions({ selectedLayer, layer, onDuplicateLayer, onDeleteLayer, onToggleLayerVisibility }) {
    if (!selectedLayer) {
        return null;
    }

    return (
        <div className="card-studio-inspector__toolbarRow">
            <button type="button" className="card-studio-inspector__utilityBtn" onClick={() => onDuplicateLayer?.()}>
                Duplicate
            </button>
            <button type="button" className="card-studio-inspector__utilityBtn" onClick={() => onToggleLayerVisibility?.(selectedLayer)}>
                {layer?.visible === false ? 'Show' : 'Hide'}
            </button>
            <button type="button" className="card-studio-inspector__utilityBtn" onClick={() => onDeleteLayer?.()}>
                Delete
            </button>
        </div>
    );
}

function SurfaceControls({ surface, onChange }) {
    return (
        <div className="card-studio-inspector__sectionBlock">
            <div className="card-studio-inspector__sectionHeading">
                <strong>Card surface</strong>
                <small>Background, outline and corner radius.</small>
            </div>
            <div className="card-studio-inspector__grid">
                <ColorField label="Background" value={surface?.fill} onChange={(value) => onChange({ fill: value, gradientStops: null })} fallback="#FFFFFF" />
                <ColorField label="Outline" value={surface?.stroke} onChange={(value) => onChange({ stroke: value })} fallback="#E8EEF5" />
                <NumberField label="Radius" value={surface?.radius || 30} onChange={(value) => onChange({ radius: value })} min={0} />
                <RangeField label="Shadow" value={surface?.shadowOpacity ?? 0.12} onChange={(value) => onChange({ shadowOpacity: value })} />
            </div>
        </div>
    );
}

function LayerStyleControls({ layer, onChange }) {
    if (!layer) {
        return (
            <div className="card-studio-inspector__hint card-studio-inspector__hint--plain">
                Pick a layer on the canvas to unlock styling controls.
            </div>
        );
    }

    const textLayers = ['title', 'summary', 'body', 'date', 'freeText'];
    const sourceKey = layer.sourceKey || layer.id;

    return (
        <div className="card-studio-inspector__sectionBlock">
            <div className="card-studio-inspector__sectionHeading">
                <strong>{sourceKey} layer</strong>
                <small>Direct controls for the selected element.</small>
            </div>

            <div className="card-studio-inspector__grid">
                <NumberField label="X" value={layer.x} onChange={(value) => onChange({ x: value })} />
                <NumberField label="Y" value={layer.y} onChange={(value) => onChange({ y: value })} />
                {'width' in layer ? <NumberField label="Width" value={layer.width} onChange={(value) => onChange({ width: value })} min={40} /> : null}
                {'height' in layer ? <NumberField label="Height" value={layer.height} onChange={(value) => onChange({ height: value })} min={32} /> : null}
            </div>

            {textLayers.includes(sourceKey) ? (
                <div className="card-studio-inspector__grid">
                    <ColorField label="Text color" value={layer.fill} onChange={(value) => onChange({ fill: value })} fallback={sourceKey === 'date' ? '#8F9FB2' : '#0B2135'} />
                    <label className="card-studio-inspector__field">
                        <span>Font</span>
                        <select value={layer.fontFamily || 'Poppins'} onChange={(event) => onChange({ fontFamily: event.target.value })}>
                            {FONT_OPTIONS.map((font) => (
                                <option key={font} value={font}>
                                    {font}
                                </option>
                            ))}
                        </select>
                    </label>
                    {'fontSize' in layer ? <NumberField label="Font size" value={layer.fontSize} onChange={(value) => onChange({ fontSize: value })} min={10} /> : null}
                    {'lineHeight' in layer ? <NumberField label="Line height" value={layer.lineHeight || 1.2} onChange={(value) => onChange({ lineHeight: value })} min={0.8} step={0.05} /> : null}
                </div>
            ) : null}

            {sourceKey === 'badge' ? (
                <div className="card-studio-inspector__grid">
                    <ColorField label="Badge fill" value={layer.backgroundFill} onChange={(value) => onChange({ backgroundFill: value })} fallback="#EEF5F8" />
                    <ColorField label="Badge text" value={layer.textColor || layer.fill} onChange={(value) => onChange({ textColor: value })} fallback="#0B2135" />
                    <NumberField label="Radius" value={layer.radius || 999} onChange={(value) => onChange({ radius: value })} min={0} />
                    <NumberField label="Font size" value={layer.fontSize || 16} onChange={(value) => onChange({ fontSize: value })} min={10} />
                </div>
            ) : null}

            {sourceKey === 'button' ? (
                <div className="card-studio-inspector__grid">
                    <ColorField label="Button fill" value={layer.backgroundFill} onChange={(value) => onChange({ backgroundFill: value })} fallback="#E67B32" />
                    <ColorField label="Button text" value={layer.textColor || layer.fill} onChange={(value) => onChange({ textColor: value })} fallback="#FFFFFF" />
                    <NumberField label="Radius" value={layer.radius || 999} onChange={(value) => onChange({ radius: value })} min={0} />
                    <NumberField label="Font size" value={layer.fontSize || 22} onChange={(value) => onChange({ fontSize: value })} min={10} />
                </div>
            ) : null}

            {sourceKey === 'logo' ? (
                <div className="card-studio-inspector__grid">
                    <ColorField label="Outer color" value={layer.backgroundFill} onChange={(value) => onChange({ backgroundFill: value })} fallback="#D9F4FA" />
                    <ColorField label="Accent color" value={layer.secondaryFill} onChange={(value) => onChange({ secondaryFill: value })} fallback="#FFF5C8" />
                    <ColorField label="Inner color" value={layer.innerFill} onChange={(value) => onChange({ innerFill: value })} fallback="#FFFFFF" />
                </div>
            ) : null}

            {sourceKey === 'image' ? (
                <RangeField label="Opacity" value={layer.opacity ?? 0.22} onChange={(value) => onChange({ opacity: value })} />
            ) : null}

            <label className="card-studio-inspector__toggle card-studio-inspector__toggle--inline">
                <input type="checkbox" checked={layer.visible !== false} onChange={(event) => onChange({ visible: event.target.checked })} />
                <span>Visible on canvas</span>
            </label>
        </div>
    );
}

function AddTextLayerControls({ onAddTextLayer }) {
    return (
        <div className="card-studio-inspector__sectionBlock card-studio-inspector__sectionBlock--accent">
            <div className="card-studio-inspector__sectionHeading">
                <strong>Add text box</strong>
                <small>Create independent text layers, then move, resize and edit them directly on the canvas.</small>
            </div>
            <div className="card-studio-inspector__actionGrid card-studio-inspector__actionGrid--three">
                <button type="button" className="card-studio-inspector__utilityBtn" onClick={() => onAddTextLayer?.('headline')}>
                    <FiPlus /> Headline
                </button>
                <button type="button" className="card-studio-inspector__utilityBtn" onClick={() => onAddTextLayer?.('body')}>
                    <FiPlus /> Body
                </button>
                <button type="button" className="card-studio-inspector__utilityBtn" onClick={() => onAddTextLayer?.('label')}>
                    <FiPlus /> Label
                </button>
            </div>
        </div>
    );
}

function FreeTextContentControls({ layer, onChange }) {
    if ((layer?.sourceKey || layer?.id) !== 'freeText') {
        return null;
    }

    return (
        <div className="card-studio-inspector__sectionBlock">
            <div className="card-studio-inspector__sectionHeading">
                <strong>Selected text box</strong>
                <small>Edit the custom text layer content without hunting on the canvas.</small>
            </div>
            <label className="card-studio-inspector__field card-studio-inspector__field--wide">
                <span>Text</span>
                <textarea rows="4" value={layer.text || ''} onChange={(event) => onChange({ text: event.target.value })} />
            </label>
        </div>
    );
}

function TypographyPresets({ onApply }) {
    return (
        <div className="card-studio-inspector__sectionBlock">
            <div className="card-studio-inspector__sectionHeading">
                <strong>Typography presets</strong>
                <small>Apply a polished text system to the selected layers.</small>
            </div>
            <div className="card-studio-inspector__presetGrid">
                {TYPOGRAPHY_PRESETS.map((preset) => (
                    <button key={preset.label} type="button" className="card-studio-inspector__presetCard" onClick={() => onApply(preset)}>
                        <span>{preset.label}</span>
                        <small>{preset.fontFamily}</small>
                    </button>
                ))}
            </div>
        </div>
    );
}

function PalettePresets({ onApply }) {
    return (
        <div className="card-studio-inspector__sectionBlock">
            <div className="card-studio-inspector__sectionHeading">
                <strong>Theme palettes</strong>
                <small>Apply a visible theme to the surface, text, badge and CTA.</small>
            </div>
            <div className="card-studio-inspector__paletteGrid">
                {PALETTE_PRESETS.map((palette) => (
                    <button key={palette.label} type="button" className="card-studio-inspector__paletteCard" onClick={() => onApply(palette)}>
                        <div className="card-studio-inspector__paletteSwatches">
                            <span style={{ background: palette.surface }} />
                            <span style={{ background: palette.badgeFill }} />
                            <span style={{ background: palette.buttonFill }} />
                            <span style={{ background: palette.primary }} />
                        </div>
                        <strong>{palette.label}</strong>
                        <small>Surface + type + CTA</small>
                    </button>
                ))}
            </div>
        </div>
    );
}

function BadgeVariantControls({ form, layout, onUpdateField, onUpdateLayout }) {
    const badgeLayer = layout?.elements?.badge || layout?.detail?.elements?.badge;
    const customFill = badgeLayer?.backgroundFill || '#E8EEF8';
    const customText = badgeLayer?.textColor || badgeLayer?.fill || '#0B2135';
    const handleVariantChange = (nextVariant) => {
        onUpdateField('badgeVariant', nextVariant);

        if (nextVariant !== 'custom') {
            onUpdateLayout('badge', {
                backgroundFill: null,
                textColor: null,
                fill: null,
            });
        }
    };

    return (
        <div className="card-studio-inspector__sectionBlock">
            <div className="card-studio-inspector__sectionHeading">
                <strong>Badge style</strong>
                <small>Choose a simple color name, or switch to Custom and pick any color you want.</small>
            </div>
            <label className="card-studio-inspector__field">
                <span>Badge style</span>
                <select value={form.badgeVariant} onChange={(event) => handleVariantChange(event.target.value)}>
                    {BADGE_VARIANTS.map((option) => (
                        <option key={option.value} value={option.value}>
                            {option.label}
                        </option>
                    ))}
                </select>
            </label>
            {form.badgeVariant === 'custom' ? (
                <div className="card-studio-inspector__grid">
                    <ColorField
                        label="Badge fill"
                        value={customFill}
                        onChange={(value) => onUpdateLayout('badge', { backgroundFill: value })}
                        fallback="#E8EEF8"
                    />
                    <ColorField
                        label="Badge text"
                        value={customText}
                        onChange={(value) => onUpdateLayout('badge', { textColor: value, fill: value })}
                        fallback="#0B2135"
                    />
                </div>
            ) : null}
        </div>
    );
}

function LayersPanel({
    studioLayers,
    selectedLayers,
    onSelectLayer,
    onToggleLayerVisibility,
    onArrangeLayer,
    onReorderLayer,
    onAlignSelection,
    onDistributeSelection,
    onArrangeSelection,
    draggedLayerId,
    layerDropTarget,
}) {
    return (
        <div className="card-studio-inspector__stack">
            <div className="card-studio-inspector__sectionBlock">
                <div className="card-studio-inspector__sectionHeading">
                    <strong>Layers</strong>
                    <small>Shift-click on canvas to multi-select. Order and visibility live here.</small>
                </div>
                <div className="card-studio-inspector__layerList">
                    {studioLayers.map((layer) => (
                        <button
                            key={layer.id}
                            type="button"
                            draggable
                            className={`card-studio-inspector__layerRow ${selectedLayers.includes(layer.id) ? 'is-active' : ''} ${draggedLayerId === layer.id ? 'is-dragging' : ''} ${layerDropTarget === layer.id ? 'is-drop-target' : ''}`}
                            onClick={(event) => onSelectLayer(layer.id, { append: event.shiftKey })}
                            onDragStart={() => onReorderLayer?.(layer.id, layer.id, 'start')}
                            onDragEnd={() => onReorderLayer?.(layer.id, layer.id, 'cancel')}
                            onDragOver={(event) => {
                                event.preventDefault();
                                onReorderLayer?.(draggedLayerId, layer.id, 'hover');
                            }}
                            onDrop={(event) => {
                                event.preventDefault();
                                onReorderLayer?.(draggedLayerId, layer.id, 'before');
                            }}
                        >
                            <span className="card-studio-inspector__layerHandle" aria-hidden="true">
                                <FiMenu />
                            </span>
                            <div className="card-studio-inspector__layerCopy">
                                <strong>{layer.label}</strong>
                                <span>{layer.visible === false ? 'Hidden on canvas' : 'Visible on canvas'} · Z{layer.stackPosition}</span>
                            </div>
                            <div className="card-studio-inspector__layerRowTools">
                                <button type="button" className="card-studio-inspector__layerIconBtn" onClick={(event) => { event.stopPropagation(); onToggleLayerVisibility?.(layer.id); }}>
                                    {layer.visible === false ? <FiEye /> : <FiEyeOff />}
                                </button>
                                <button type="button" className="card-studio-inspector__layerIconBtn" onClick={(event) => { event.stopPropagation(); onArrangeLayer?.(layer.id, 'forward'); }}>
                                    <FiArrowUp />
                                </button>
                                <button type="button" className="card-studio-inspector__layerIconBtn" onClick={(event) => { event.stopPropagation(); onArrangeLayer?.(layer.id, 'backward'); }}>
                                    <FiArrowDown />
                                </button>
                            </div>
                        </button>
                    ))}
                </div>
            </div>

            <div className="card-studio-inspector__sectionBlock">
                <div className="card-studio-inspector__sectionHeading">
                    <strong>Align</strong>
                    <small>Snap selected layers against the card frame.</small>
                </div>
                <div className="card-studio-inspector__actionGrid card-studio-inspector__actionGrid--three">
                    {[
                        ['left', 'Left'],
                        ['center', 'Center'],
                        ['right', 'Right'],
                        ['top', 'Top'],
                        ['middle', 'Middle'],
                        ['bottom', 'Bottom'],
                    ].map(([value, label]) => (
                        <button key={value} type="button" className="card-studio-inspector__utilityBtn" onClick={() => onAlignSelection(value)}>
                            {label}
                        </button>
                    ))}
                </div>
                <div className="card-studio-inspector__actionGrid card-studio-inspector__actionGrid--two">
                    <button type="button" className="card-studio-inspector__utilityBtn" onClick={() => onDistributeSelection('horizontal')}>
                        Distribute X
                    </button>
                    <button type="button" className="card-studio-inspector__utilityBtn" onClick={() => onDistributeSelection('vertical')}>
                        Distribute Y
                    </button>
                </div>
            </div>

            <div className="card-studio-inspector__sectionBlock">
                <div className="card-studio-inspector__sectionHeading">
                    <strong>Arrange</strong>
                    <small>Control visual stacking like a real layers panel.</small>
                </div>
                <div className="card-studio-inspector__actionGrid card-studio-inspector__actionGrid--two">
                    <button type="button" className="card-studio-inspector__utilityBtn" onClick={() => onArrangeSelection('front')}>
                        Bring front
                    </button>
                    <button type="button" className="card-studio-inspector__utilityBtn" onClick={() => onArrangeSelection('back')}>
                        Send back
                    </button>
                    <button type="button" className="card-studio-inspector__utilityBtn" onClick={() => onArrangeSelection('forward')}>
                        Forward
                    </button>
                    <button type="button" className="card-studio-inspector__utilityBtn" onClick={() => onArrangeSelection('backward')}>
                        Backward
                    </button>
                </div>
            </div>
        </div>
    );
}

function HistoryPanel({ historyEntries = [], onRestore }) {
    return (
        <div className="card-studio-inspector__sectionBlock">
            <div className="card-studio-inspector__sectionHeading">
                <strong>Recent changes</strong>
                <small>Visual history of the latest editor actions.</small>
            </div>
            <div className="card-studio-inspector__historyList">
                {historyEntries.length ? historyEntries.map((entry, index) => (
                    <div key={`${entry.createdAt}-${index}`} className="card-studio-inspector__historyItem">
                        <div className="card-studio-inspector__historyPreview" />
                        <div className="card-studio-inspector__historyCopy">
                            <strong>{entry.label || 'Canvas edit'}</strong>
                            <span><FiClock /> {new Date(entry.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}</span>
                            <small>{entry.form?.title || 'Untitled card'}</small>
                        </div>
                        <button type="button" className="card-studio-inspector__historyBtn" onClick={() => onRestore?.(entry)}>
                            Restore
                        </button>
                    </div>
                )) : (
                    <div className="card-studio-inspector__hint card-studio-inspector__hint--plain">Your history stack will appear here as soon as you start editing.</div>
                )}
            </div>
        </div>
    );
}

export default function CardStudioInspector({
    form,
    layout,
    viewMode,
    onChangeViewMode,
    editorMode: controlledEditorMode,
    onEditorModeChange,
    showModeTabs = true,
    selectedLayer,
    selectedLayers = [],
    studioLayers = [],
    onSelectLayer,
    onUpdateField,
    onUpdateLayout,
    onUpload,
    onUpdateDetailImageUrl,
    isUploading,
    onResetTemplate,
    onDuplicateLayer,
    onDeleteLayer,
    onToggleLayerVisibility,
    onArrangeLayer,
    onReorderLayer,
    onConvertBuiltInModal,
    onUpdateCardSurface,
    onAlignSelection,
    onDistributeSelection,
    onArrangeSelection,
    onApplyTypographyPreset,
    onApplyPalettePreset,
    onAddTextLayer,
    onApplyImageFramePreset,
    onNudgeImageCrop,
    draggedLayerId,
    layerDropTarget,
    historyEntries = [],
    onRestoreHistoryEntry,
}) {
    const currentLogo = getContentIcon(form);
    const assetPresets = ICON_OPTIONS.filter((option) => option.value && option.value !== 'uploaded-image');
    const [internalEditorMode, setInternalEditorMode] = useState('text');
    const editorMode = controlledEditorMode || internalEditorMode;
    const setEditorMode = onEditorModeChange || setInternalEditorMode;

    const activeLayer = selectedLayer || 'title';
    const builtInDetail = useMemo(
        () => (form.actionType === 'modal' && form.actionValue
            ? createEditableDetailFromBuiltin(form.actionValue, form.title)
            : null),
        [form.actionType, form.actionValue, form.title],
    );
    const layerSource = viewMode === 'detail'
        ? {
              surface: layout?.detail?.modal,
              elements: layout?.detail?.elements || {},
              customElements: layout?.detail?.customElements || [],
          }
        : {
              surface: layout?.card,
              elements: layout?.elements || {},
              customElements: layout?.customElements || [],
          };

    const layer = layerSource.elements?.[activeLayer] || layerSource.customElements?.find((item) => item.id === activeLayer) || null;
    const detailImageUrl = layout?.detail?.imageUrl || '';
    const activeImageUrl = viewMode === 'detail' ? detailImageUrl : form.imageUrl;
    const activeImageLabel = viewMode === 'detail' ? 'Pop-up image' : 'Card image';
    const activeImageHelper = viewMode === 'detail'
        ? 'Only used inside this pop-up. If empty, the pop-up falls back to the card image.'
        : 'Used by the main card. The pop-up can use its own image separately.';
    const handleActiveImageChange = (value) => {
        if (viewMode === 'detail') {
            onUpdateDetailImageUrl?.(value);
            return;
        }

        onUpdateField('imageUrl', value);
    };

    useEffect(() => {
        if (!selectedLayer) {
            return;
        }

        if (['logo', 'image'].includes(selectedLayer)) {
            setEditorMode('media');
            return;
        }

        if (selectedLayer === 'button') {
            setEditorMode('action');
            return;
        }

        if (['badge', 'title', 'summary', 'date', 'body'].includes(selectedLayer)) {
            setEditorMode('text');
        }
    }, [selectedLayer]);

    const selectedLayerLabel = selectedLayers.length > 1
        ? `${selectedLayers.length} layers selected`
        : studioLayers.find((item) => item.id === activeLayer)?.label || 'Headline';

    return (
        <aside className="card-studio-inspector">
            <div className="card-studio-inspector__header">
                <p className="card-studio-inspector__eyebrow">
                    <FiLayers /> Editor
                </p>
                <h3>{viewMode === 'detail' ? 'Pop-up editor' : 'Card editor'}</h3>
                <p>Focus the card first. Pick a layer, then edit content or style.</p>
            </div>
            <div className="card-studio-inspector__bodyScroll">
                <div className="card-studio-inspector__sectionBlock">
                    <div className="card-studio-inspector__actionGrid card-studio-inspector__actionGrid--two">
                        <button
                            type="button"
                            className={`card-studio-inspector__actionCard ${viewMode === 'card' ? 'is-active' : ''}`}
                            onClick={() => onChangeViewMode('card')}
                        >
                            <strong>Card</strong>
                            <small>Main card on Home.</small>
                        </button>
                        <button
                            type="button"
                            className={`card-studio-inspector__actionCard ${viewMode === 'detail' ? 'is-active' : ''}`}
                            onClick={() => onChangeViewMode('detail')}
                            disabled={!['detail', 'modal'].includes(form.actionType)}
                        >
                            <strong>Pop-up</strong>
                            <small>
                                {form.actionType === 'detail'
                                    ? 'Editable detail view.'
                                    : form.actionType === 'modal'
                                        ? 'Preview the prebuilt pop-up.'
                                        : 'Enable detail action first.'}
                            </small>
                        </button>
                    </div>
                    <span className="card-studio-inspector__activeLayerTag">Editing {selectedLayerLabel}</span>
                    <LayerQuickActions
                        selectedLayer={selectedLayer}
                        layer={layer}
                        onDuplicateLayer={onDuplicateLayer}
                        onDeleteLayer={onDeleteLayer}
                        onToggleLayerVisibility={onToggleLayerVisibility}
                    />
                </div>

                {showModeTabs ? (
                    <div className="card-studio-inspector__modeGrid card-studio-inspector__modeGrid--tabs">
                        {Object.entries(PANEL_META).map(([key, meta]) => {
                            const Icon = meta.icon;
                            return (
                                <button
                                    key={key}
                                    type="button"
                                    className={`card-studio-inspector__modeTab ${editorMode === key ? 'is-active' : ''}`}
                                    onClick={() => setEditorMode(key)}
                                >
                                    <Icon />
                                    <span>{meta.label}</span>
                                </button>
                            );
                        })}
                    </div>
                ) : null}

                {editorMode === 'text' ? (
                    <div className="card-studio-inspector__stack">
                    <AddTextLayerControls onAddTextLayer={onAddTextLayer} />
                    <FreeTextContentControls layer={layer} onChange={(patch) => onUpdateLayout(activeLayer, patch)} />
                    <div className="card-studio-inspector__sectionBlock">
                        <div className="card-studio-inspector__sectionHeading">
                            <strong>Words on the card</strong>
                            <small>Short labels, headline, summary and date.</small>
                        </div>
                        <div className="card-studio-inspector__grid">
                            {viewMode === 'card' ? (
                                <>
                                    <label className="card-studio-inspector__field">
                                        <span>Badge text</span>
                                        <input value={form.badgeText} onChange={(event) => onUpdateField('badgeText', event.target.value)} placeholder="Update" />
                                    </label>
                                    <label className="card-studio-inspector__field card-studio-inspector__field--wide">
                                        <span>Headline</span>
                                        <textarea rows="3" value={form.title} onChange={(event) => onUpdateField('title', event.target.value)} placeholder="Main message" />
                                    </label>
                                    <div className="card-studio-inspector__field card-studio-inspector__field--wide card-studio-inspector__field--editor">
                                        <span>Summary</span>
                                        <RichTextEditor value={form.descriptionHtml} onChange={(value) => onUpdateField('descriptionHtml', value)} />
                                    </div>
                                    <label className="card-studio-inspector__field">
                                        <span>Date</span>
                                        <input value={form.dateText} onChange={(event) => onUpdateField('dateText', event.target.value)} placeholder="February 5, 2026" />
                                    </label>
                                    <label className="card-studio-inspector__field">
                                        <span>Button label</span>
                                        <input value={form.buttonText} onChange={(event) => onUpdateField('buttonText', event.target.value)} placeholder="Read more" />
                                    </label>
                                </>
                            ) : (
                                <>
                                    {form.actionType === 'modal' && builtInDetail ? (
                                        <div className="card-studio-inspector__stack">
                                            <div className="card-studio-inspector__hint card-studio-inspector__hint--plain">
                                                You are previewing a prebuilt pop-up. It uses the real embedded modal with its own images and composition.
                                            </div>
                                            <label className="card-studio-inspector__field card-studio-inspector__field--wide">
                                                <span>Prebuilt pop-up title</span>
                                                <textarea rows="2" value={builtInDetail.detailTitle} readOnly />
                                            </label>
                                            <div className="card-studio-inspector__field card-studio-inspector__field--wide card-studio-inspector__field--editor">
                                                <span>Prebuilt pop-up body</span>
                                                <RichTextEditor value={builtInDetail.detailContentHtml} onChange={() => {}} readOnly />
                                            </div>
                                            <button type="button" className="card-studio-inspector__utilityBtn" onClick={onConvertBuiltInModal}>
                                                Convert prebuilt pop-up into editable version
                                            </button>
                                        </div>
                                    ) : (
                                        <>
                                            <label className="card-studio-inspector__field card-studio-inspector__field--wide">
                                                <span>Pop-up title</span>
                                                <textarea rows="2" value={form.detailTitle} onChange={(event) => onUpdateField('detailTitle', event.target.value)} placeholder="Detail title" />
                                            </label>
                                            <div className="card-studio-inspector__field card-studio-inspector__field--wide card-studio-inspector__field--editor">
                                                <span>Pop-up body</span>
                                                <RichTextEditor value={form.detailContentHtml} onChange={(value) => onUpdateField('detailContentHtml', value)} />
                                            </div>
                                        </>
                                    )}
                                </>
                            )}
                        </div>
                    </div>
                    {viewMode === 'card' ? (
                        <BadgeVariantControls form={form} layout={layout} onUpdateField={onUpdateField} onUpdateLayout={onUpdateLayout} />
                    ) : null}
                    <TypographyPresets onApply={onApplyTypographyPreset} />
                    <PalettePresets onApply={onApplyPalettePreset} />
                    </div>
                ) : null}

                {editorMode === 'style' ? (
                    <div className="card-studio-inspector__stack">
                        <SurfaceControls surface={layerSource.surface} onChange={onUpdateCardSurface} />
                        <LayerStyleControls layer={layer} onChange={(patch) => onUpdateLayout(activeLayer, patch)} />
                    </div>
                ) : null}

                {editorMode === 'layers' ? (
                    <LayersPanel
                        studioLayers={studioLayers}
                        selectedLayers={selectedLayers}
                        onSelectLayer={onSelectLayer}
                        onToggleLayerVisibility={onToggleLayerVisibility}
                        onArrangeLayer={onArrangeLayer}
                        onReorderLayer={onReorderLayer}
                        onAlignSelection={onAlignSelection}
                        onDistributeSelection={onDistributeSelection}
                        onArrangeSelection={onArrangeSelection}
                        draggedLayerId={draggedLayerId}
                        layerDropTarget={layerDropTarget}
                    />
                ) : null}

                {editorMode === 'media' ? (
                    <div className="card-studio-inspector__stack">
                    <div className="card-studio-inspector__logoCurrent">
                        <span className="card-studio-inspector__logoThumb">
                            {currentLogo ? <img src={currentLogo} alt="" /> : <FiImage />}
                        </span>
                        <div>
                            <strong>{ICON_OPTIONS.find((option) => option.value === form.iconKey)?.label || 'No logo selected'}</strong>
                            <small>Upload once, then position it directly on the canvas.</small>
                        </div>
                    </div>

                    <div className="card-studio-inspector__sectionBlock">
                        <div className="card-studio-inspector__sectionHeading">
                            <strong>Logo source</strong>
                            <small>Choose a built-in mark or reuse the uploaded image.</small>
                        </div>
                        <div className="card-studio-inspector__presetGrid">
                            <button type="button" className={`card-studio-inspector__presetCard ${!form.iconKey ? 'is-active' : ''}`} onClick={() => onUpdateField('iconKey', '')}>
                                <span className="card-studio-inspector__presetThumb card-studio-inspector__presetThumb--empty">
                                    <FiLayers />
                                </span>
                                <span>No logo</span>
                            </button>
                            <button
                                type="button"
                                draggable={Boolean(form.imageUrl)}
                                className={`card-studio-inspector__presetCard ${form.iconKey === 'uploaded-image' ? 'is-active' : ''}`}
                                onClick={() => onUpdateField('iconKey', 'uploaded-image')}
                                onDragStart={(event) => {
                                    if (!form.imageUrl) {
                                        event.preventDefault();
                                        return;
                                    }
                                    event.dataTransfer.setData(
                                        'application/x-card-studio-asset',
                                        JSON.stringify({ type: 'image-url', value: form.imageUrl, iconKey: 'uploaded-image' }),
                                    );
                                }}
                            >
                                <span className="card-studio-inspector__presetThumb">
                                    {form.imageUrl ? <img src={resolveContentAssetUrl(form.imageUrl)} alt="" /> : <FiImage />}
                                </span>
                                <span>Uploaded image</span>
                            </button>
                            {assetPresets.map((option) => (
                                <button
                                    key={option.value}
                                    type="button"
                                    draggable
                                    className={`card-studio-inspector__presetCard ${form.iconKey === option.value ? 'is-active' : ''}`}
                                    onClick={() => onUpdateField('iconKey', option.value)}
                                    onDragStart={(event) => {
                                        event.dataTransfer.setData(
                                            'application/x-card-studio-asset',
                                            JSON.stringify({ type: 'icon-key', value: option.value }),
                                        );
                                    }}
                                >
                                    <span className="card-studio-inspector__presetThumb">
                                        <img src={CONTENT_ICON_MAP[option.value]} alt="" />
                                    </span>
                                    <span>{option.label}</span>
                                </button>
                            ))}
                        </div>
                    </div>

                    <div className="card-studio-inspector__sectionBlock">
                        <div className="card-studio-inspector__sectionHeading">
                            <strong>{activeImageLabel}</strong>
                            <small>{activeImageHelper}</small>
                        </div>
                        <label className="card-studio-inspector__field">
                            <span>Image URL</span>
                            <input value={activeImageUrl || ''} onChange={(event) => handleActiveImageChange(event.target.value)} placeholder="Paste an image URL" />
                        </label>
                        <div className="card-studio-inspector__upload">
                            <button type="button" className="card-studio-inspector__uploadBtn" onClick={() => document.getElementById('card-studio-upload-input')?.click()}>
                                {isUploading ? 'Uploading...' : `Upload ${viewMode === 'detail' ? 'pop-up image' : 'card image'}`}
                            </button>
                            <input id="card-studio-upload-input" type="file" accept={IMAGE_UPLOAD_ACCEPT} hidden onChange={(event) => onUpload?.(event, viewMode === 'detail' ? 'detail' : 'card')} />
                        </div>
                        {viewMode === 'detail' ? (
                            <div className="card-studio-inspector__hint card-studio-inspector__hint--plain">
                                {detailImageUrl
                                    ? 'This pop-up is using its own image.'
                                    : 'No pop-up image set. It will use the card image until you upload or paste a different one.'}
                            </div>
                        ) : null}
                        {layer?.sourceKey === 'image' ? (
                            <>
                                <div className="card-studio-inspector__sectionHeading">
                                    <strong>Frame</strong>
                                    <small>Controls the space the image occupies on the card or pop-up.</small>
                                </div>
                                <div className="card-studio-inspector__grid">
                                    <NumberField label="Frame X" value={layer.x || 0} onChange={(value) => onUpdateLayout(activeLayer, { x: value })} min={-2000} />
                                    <NumberField label="Frame Y" value={layer.y || 0} onChange={(value) => onUpdateLayout(activeLayer, { y: value })} min={-2000} />
                                    <NumberField label="Frame W" value={layer.width || 0} onChange={(value) => onUpdateLayout(activeLayer, { width: value })} min={40} />
                                    <NumberField label="Frame H" value={layer.height || 0} onChange={(value) => onUpdateLayout(activeLayer, { height: value })} min={32} />
                                    <NumberField label="Frame angle" value={layer.rotation || 0} onChange={(value) => onUpdateLayout(activeLayer, { rotation: value })} min={-360} max={360} />
                                    <NumberField label="Mask radius" value={layer.radius || 28} onChange={(value) => onUpdateLayout(activeLayer, { radius: value })} min={0} />
                                </div>

                                <div className="card-studio-inspector__sectionHeading">
                                    <strong>Image inside frame</strong>
                                    <small>Move, stretch and rotate the actual image without changing the frame.</small>
                                </div>
                                <div className="card-studio-inspector__grid">
                                    <label className="card-studio-inspector__field">
                                        <span>Fit mode</span>
                                        <select value={layer.imageFit || 'cover'} onChange={(event) => onUpdateLayout(activeLayer, { imageFit: event.target.value })}>
                                            <option value="cover">Cover</option>
                                            <option value="contain">Contain</option>
                                        </select>
                                    </label>
                                    <NumberField label="Image X" value={layer.imageOffsetX || 0} onChange={(value) => onUpdateLayout(activeLayer, { imageOffsetX: value })} min={-2000} />
                                    <NumberField label="Image Y" value={layer.imageOffsetY || 0} onChange={(value) => onUpdateLayout(activeLayer, { imageOffsetY: value })} min={-2000} />
                                    <NumberField label="Image W scale" value={layer.imageScaleX || 1} onChange={(value) => onUpdateLayout(activeLayer, { imageScaleX: Math.max(0.05, value) })} min={0.05} step={0.05} />
                                    <NumberField label="Image H scale" value={layer.imageScaleY || 1} onChange={(value) => onUpdateLayout(activeLayer, { imageScaleY: Math.max(0.05, value) })} min={0.05} step={0.05} />
                                    <NumberField label="Image angle" value={layer.imageRotation || 0} onChange={(value) => onUpdateLayout(activeLayer, { imageRotation: value })} min={-360} max={360} />
                                    <NumberField label="Crop X" value={layer.cropX || 0} onChange={(value) => onUpdateLayout(activeLayer, { cropX: value })} min={0} />
                                    <NumberField label="Crop Y" value={layer.cropY || 0} onChange={(value) => onUpdateLayout(activeLayer, { cropY: value })} min={0} />
                                    <NumberField label="Crop W" value={layer.cropWidth || 0} onChange={(value) => onUpdateLayout(activeLayer, { cropWidth: value })} min={0} />
                                    <NumberField label="Crop H" value={layer.cropHeight || 0} onChange={(value) => onUpdateLayout(activeLayer, { cropHeight: value })} min={0} />
                                </div>
                            </>
                        ) : null}
                    </div>
                    {layer?.sourceKey === 'image' ? (
                        <div className="card-studio-inspector__sectionBlock">
                            <div className="card-studio-inspector__sectionHeading">
                                <strong>Crop and mask</strong>
                                <small>Fast framing controls for the selected image layer.</small>
                            </div>
                            <div className="card-studio-inspector__actionGrid card-studio-inspector__actionGrid--two">
                                <button type="button" className="card-studio-inspector__utilityBtn" onClick={() => onApplyImageFramePreset?.('fill')}>
                                    Fill frame
                                </button>
                                <button type="button" className="card-studio-inspector__utilityBtn" onClick={() => onApplyImageFramePreset?.('contain')}>
                                    Show full image
                                </button>
                                <button type="button" className="card-studio-inspector__utilityBtn" onClick={() => onApplyImageFramePreset?.('square')}>
                                    Square mask
                                </button>
                                <button type="button" className="card-studio-inspector__utilityBtn" onClick={() => onApplyImageFramePreset?.('soft')}>
                                    Soft corners
                                </button>
                                <button type="button" className="card-studio-inspector__utilityBtn" onClick={() => onApplyImageFramePreset?.('round')}>
                                    Pill mask
                                </button>
                                <button type="button" className="card-studio-inspector__utilityBtn" onClick={() => onApplyImageFramePreset?.('reset')}>
                                    Reset crop
                                </button>
                                <button type="button" className="card-studio-inspector__utilityBtn" onClick={() => onNudgeImageCrop?.(-12, 0, 0)}>
                                    Crop left
                                </button>
                                <button type="button" className="card-studio-inspector__utilityBtn" onClick={() => onNudgeImageCrop?.(12, 0, 0)}>
                                    Crop right
                                </button>
                                <button type="button" className="card-studio-inspector__utilityBtn" onClick={() => onNudgeImageCrop?.(0, -12, 0)}>
                                    Crop up
                                </button>
                                <button type="button" className="card-studio-inspector__utilityBtn" onClick={() => onNudgeImageCrop?.(0, 12, 0)}>
                                    Crop down
                                </button>
                                <button type="button" className="card-studio-inspector__utilityBtn" onClick={() => onNudgeImageCrop?.(0, 0, 18)}>
                                    Zoom crop
                                </button>
                            </div>
                        </div>
                    ) : null}
                    </div>
                ) : null}

                {editorMode === 'action' ? (
                    <div className="card-studio-inspector__stack">
                    <label className="card-studio-inspector__field">
                        <span>Button label</span>
                        <input value={form.buttonText} onChange={(event) => onUpdateField('buttonText', event.target.value)} placeholder="Read more" />
                    </label>
                    <div className="card-studio-inspector__sectionBlock">
                        <div className="card-studio-inspector__sectionHeading">
                            <strong>Click behavior</strong>
                            <small>Choose what opens when someone clicks the CTA.</small>
                        </div>
                        <div className="card-studio-inspector__actionGrid">
                            {ACTION_TYPES.map((option) => (
                                <button
                                    key={option.value}
                                    type="button"
                                    className={`card-studio-inspector__actionCard ${form.actionType === option.value ? 'is-active' : ''}`}
                                    onClick={() => onUpdateField('actionType', option.value)}
                                >
                                    <strong>{option.label}</strong>
                                </button>
                            ))}
                        </div>
                    </div>

                    {form.actionType === 'modal' ? (
                        <label className="card-studio-inspector__field">
                            <span>Prebuilt pop-up template</span>
                            <select value={form.actionValue} onChange={(event) => onUpdateField('actionValue', event.target.value)}>
                                <option value="">Choose one</option>
                                {MODAL_OPTIONS.map((option) => (
                                    <option key={option.value} value={option.value}>
                                        {option.label}
                                    </option>
                                ))}
                            </select>
                        </label>
                    ) : null}

                    {form.actionType === 'detail' ? (
                        <div className="card-studio-inspector__hint card-studio-inspector__hint--plain">
                            This option opens a pop-up designed specifically for this card. Switch to the Pop-up tab above to edit its title, body and layout.
                        </div>
                    ) : null}

                    {form.actionType === 'modal' && form.actionValue ? (
                        <button type="button" className="card-studio-inspector__utilityBtn" onClick={onConvertBuiltInModal}>
                            Convert prebuilt pop-up into editable version
                        </button>
                    ) : null}

                    {form.actionType === 'route' ? (
                        <label className="card-studio-inspector__field">
                            <span>App route</span>
                            <input value={form.actionValue} onChange={(event) => onUpdateField('actionValue', event.target.value)} placeholder="/ActivePosition" />
                        </label>
                    ) : null}

                    {form.actionType === 'url' ? (
                        <label className="card-studio-inspector__field">
                            <span>External URL</span>
                            <input value={form.actionValue} onChange={(event) => onUpdateField('actionValue', event.target.value)} placeholder="https://example.com" />
                        </label>
                    ) : null}
                    </div>
                ) : null}

                {editorMode === 'publish' ? (
                    <div className="card-studio-inspector__stack">
                    <div className="card-studio-inspector__grid">
                        <label className="card-studio-inspector__field">
                            <span>Section</span>
                            <select value={form.section} onChange={(event) => onUpdateField('section', event.target.value)}>
                                <option value={CONTENT_SECTIONS.spotlight}>Spotlight</option>
                                <option value={CONTENT_SECTIONS.programNews}>Program News</option>
                            </select>
                        </label>
                        <NumberField label="Order" value={form.displayOrder} onChange={(value) => onUpdateField('displayOrder', value || 1)} min={1} />
                    </div>

                    <label className="card-studio-inspector__toggle card-studio-inspector__toggle--inline">
                        <input type="checkbox" checked={form.isPublished} onChange={(event) => onUpdateField('isPublished', event.target.checked)} />
                        <span>Published</span>
                    </label>

                    <div className="card-studio-inspector__grid">
                        <label className="card-studio-inspector__field">
                            <span>Show from</span>
                            <input type="datetime-local" value={form.publishStartUtc || ''} onChange={(event) => onUpdateField('publishStartUtc', event.target.value)} />
                        </label>
                        <label className="card-studio-inspector__field">
                            <span>Hide after</span>
                            <input type="datetime-local" value={form.publishEndUtc || ''} onChange={(event) => onUpdateField('publishEndUtc', event.target.value)} />
                        </label>
                    </div>

                    <button type="button" className="card-studio-inspector__utilityBtn" onClick={onResetTemplate}>
                        Reset template
                    </button>
                    <HistoryPanel historyEntries={historyEntries} onRestore={onRestoreHistoryEntry} />
                    </div>
                ) : null}
            </div>
        </aside>
    );
}
