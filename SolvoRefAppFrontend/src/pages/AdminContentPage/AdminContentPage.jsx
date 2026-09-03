import { useEffect, useLayoutEffect, useMemo, useRef, useState } from 'react';
import DOMPurify from 'dompurify';
import { gsap } from 'gsap';
import { FiChevronDown, FiChevronLeft, FiChevronRight, FiChevronUp, FiCopy, FiCornerDownLeft, FiCornerDownRight, FiEye, FiEyeOff, FiGrid, FiImage, FiLayers, FiMousePointer, FiPlus, FiRefreshCw, FiSave, FiSearch, FiSettings, FiShield, FiTrash2, FiType, FiUserPlus, FiUsers, FiX } from 'react-icons/fi';
import AnimatedPageShell from '../../components/AnimatedPageShell';
import CardStudioCanvas from '../../components/CardStudio/CardStudioCanvas';
import CardStudioInspector from '../../components/CardStudio/CardStudioInspector';
import HomePromoModal from '../../components/HomePromoModal/HomePromoModal';
import { createEditableDetailFromBuiltin, getHomePromoBuiltin } from '../../content/homePromoBuiltins';
import {
    CARD_STUDIO_LAYERS,
    createDefaultLayout,
    parseLayoutJson,
    serializeLayout,
} from '../../components/CardStudio/cardStudioTemplates';
import { usePageMotion } from '../../animations/usePageMotion';
import { useSideBar } from '../../Context/SideBar/SideBarContext';
import { ACTION_TYPES, BADGE_VARIANTS, CONTENT_ICON_MAP, CONTENT_SECTIONS, ICON_OPTIONS } from '../../content/homeContentConfig';
import { resolveContentAssetUrl } from '../../config/api';
import {
    activateAdminUser,
    createAdminUser,
    deactivateAdminUser,
    deleteContentCard,
    fetchAdminUsers,
    fetchContentCards,
    removeAdminUser,
    saveContentCard,
    uploadContentImage,
} from '../../services/contentService';
import './AdminContentPage.css';
import 'react-quill-new/dist/quill.snow.css';

const ASSET_LIBRARY_STORAGE_KEY = 'solvo-card-studio-assets-v1';

const SECTION_META = {
    [CONTENT_SECTIONS.spotlight]: {
        label: 'Spotlight cards',
        description: 'Cards layered under the hero banner.',
    },
    [CONTENT_SECTIONS.programNews]: {
        label: 'Program News',
        description: 'Cards used by the pinned scroll sequence.',
    },
};

const WORKSPACE_TOOLS = [
    {
        key: 'cards',
        label: 'Cards',
        icon: FiGrid,
        title: 'Card library',
        description: 'Browse, search and jump into the exact card you want to edit.',
    },
    {
        key: 'text',
        label: 'Text',
        icon: FiType,
        title: 'Text system',
        description: 'Focus on copy, hierarchy and typography for the selected layer.',
    },
    {
        key: 'style',
        label: 'Style',
        icon: FiSettings,
        title: 'Style controls',
        description: 'Adjust surface, badge and supporting visual treatments.',
    },
    {
        key: 'media',
        label: 'Media',
        icon: FiImage,
        title: 'Media tools',
        description: 'Upload, drop and frame the visual assets used by the card.',
    },
    {
        key: 'layers',
        label: 'Layers',
        icon: FiLayers,
        title: 'Layer stack',
        description: 'Select, align and arrange elements like a real design tool.',
    },
    {
        key: 'action',
        label: 'Action',
        icon: FiMousePointer,
        title: 'Action settings',
        description: 'Control what the card opens and how the CTA behaves.',
    },
    {
        key: 'publish',
        label: 'Publish',
        icon: FiShield,
        title: 'Publish settings',
        description: 'Review state, history and release readiness before saving.',
    },
    {
        key: 'admins',
        label: 'Admins',
        icon: FiUsers,
        title: 'Admin access',
        description: 'Create, deactivate and remove admin access by email.',
    },
];

const WORKSPACE_TYPOGRAPHY_PRESETS = [
    { label: 'Headline', fontFamily: 'Poppins', fontSize: 46, lineHeight: 1.06, fill: '#0B2135' },
    { label: 'Editorial', fontFamily: 'Merriweather', fontSize: 42, lineHeight: 1.12, fill: '#0E2237' },
    { label: 'Compact', fontFamily: 'DM Sans', fontSize: 38, lineHeight: 1.08, fill: '#102A43' },
];

const WORKSPACE_PALETTE_PRESETS = [
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

const WORKSPACE_PANEL_SECTIONS = {
    text: [
        { key: 'copy', label: 'Copy' },
        { key: 'type', label: 'Type' },
        { key: 'palette', label: 'Palette' },
    ],
    style: [
        { key: 'surface', label: 'Surface' },
        { key: 'badge', label: 'Badge' },
    ],
    media: [
        { key: 'library', label: 'Library' },
        { key: 'frame', label: 'Crop' },
    ],
    layers: [
        { key: 'stack', label: 'Stack' },
        { key: 'align', label: 'Align' },
    ],
    action: [
        { key: 'cta', label: 'CTA' },
        { key: 'view', label: 'View' },
    ],
    publish: [
        { key: 'state', label: 'State' },
        { key: 'history', label: 'History' },
    ],
};

const DEFAULT_PANEL_SECTION = {
    text: 'copy',
    style: 'surface',
    media: 'library',
    layers: 'stack',
    action: 'cta',
    publish: 'state',
};

function getOrderedLayerEntries(target, baseDefinitions) {
    const baseEntries = Object.entries(target?.elements || {}).map(([id, item], index) => {
        const definition = baseDefinitions.find((layer) => layer.key === id);
        return {
            id,
            label: definition?.label || id,
            sourceKey: id,
            visible: item?.visible !== false,
            isCustom: false,
            zIndex: Number.isFinite(item?.zIndex) ? item.zIndex : index,
        };
    });

    const customEntries = (target?.customElements || []).map((item, index) => ({
        id: item.id,
        label: item.sourceKey === 'freeText'
            ? (item.text || `Text box ${index + 1}`).slice(0, 28)
            : `${baseDefinitions.find((layer) => layer.key === item.sourceKey)?.label || item.sourceKey} copy ${index + 1}`,
        sourceKey: item.sourceKey,
        visible: item.visible !== false,
        isCustom: true,
        zIndex: Number.isFinite(item.zIndex) ? item.zIndex : 100 + index,
    }));

    return [...baseEntries, ...customEntries]
        .sort((left, right) => {
            if (right.zIndex !== left.zIndex) {
                return right.zIndex - left.zIndex;
            }

            return left.label.localeCompare(right.label);
        })
        .map((layer, index, ordered) => ({
            ...layer,
            stackPosition: ordered.length - index,
        }));
}

function resequenceLayerStack(target, orderedIds) {
    const nextZIndex = new Map();
    const total = orderedIds.length;

    orderedIds.forEach((id, index) => {
        nextZIndex.set(id, total - index);
    });

    return {
        ...target,
        elements: Object.fromEntries(
            Object.entries(target.elements || {}).map(([key, value]) => [
                key,
                nextZIndex.has(key)
                    ? {
                          ...value,
                          zIndex: nextZIndex.get(key),
                      }
                    : value,
            ]),
        ),
        customElements: (target.customElements || []).map((item) =>
            nextZIndex.has(item.id)
                ? {
                      ...item,
                      zIndex: nextZIndex.get(item.id),
                  }
                : item,
        ),
    };
}

function mergePersistentAssets(existingAssets = [], incomingAssets = []) {
    const merged = [...incomingAssets, ...existingAssets]
        .filter((item) => item?.preview && item?.label)
        .reduce((acc, item) => {
            const key = `${item.type}:${item.value}`;
            if (!acc.map.has(key)) {
                acc.map.set(key, true);
                acc.items.push({
                    id: item.id || key,
                    label: item.label,
                    type: item.type,
                    preview: item.preview,
                    value: item.value,
                    iconKey: item.iconKey,
                    createdAt: item.createdAt || Date.now(),
                });
            }
            return acc;
        }, { map: new Map(), items: [] })
        .items
        .slice(0, 20);

    return merged;
}

function normalizeImageUrl(value) {
    return resolveContentAssetUrl((value || '').trim());
}

function createEmptyCard(section = CONTENT_SECTIONS.spotlight) {
    return {
        id: null,
        section,
        badgeText: section === CONTENT_SECTIONS.programNews ? 'Update' : '',
        badgeVariant: section === CONTENT_SECTIONS.programNews ? 'update' : 'mint',
        title: '',
        descriptionHtml: '',
        dateText: section === CONTENT_SECTIONS.programNews ? 'February 5, 2026' : '',
        buttonText: section === CONTENT_SECTIONS.programNews ? 'Read More' : 'Learn more',
        actionType: 'none',
        actionValue: '',
        iconKey: section === CONTENT_SECTIONS.spotlight ? 'spotlight-incentive' : '',
        imageUrl: '',
        detailTitle: '',
        detailContentHtml: '',
        displayOrder: 1,
        isPublished: true,
        publishStartUtc: '',
        publishEndUtc: '',
        layoutJson: '',
    };
}

function normalizeCard(card, section) {
    return {
        ...createEmptyCard(section),
        ...card,
        imageUrl: normalizeImageUrl(card?.imageUrl || ''),
        publishStartUtc: card?.publishStartUtc ? card.publishStartUtc.slice(0, 16) : '',
        publishEndUtc: card?.publishEndUtc ? card.publishEndUtc.slice(0, 16) : '',
        layoutJson: card?.layoutJson || '',
    };
}

function sortCards(items) {
    return [...items].sort((a, b) => {
        if (a.section !== b.section) {
            return a.section.localeCompare(b.section);
        }
        return (a.displayOrder || 0) - (b.displayOrder || 0);
    });
}

function cloneSnapshot(form, layout, label = 'Canvas edit') {
    return {
        form: JSON.parse(JSON.stringify(form)),
        layout: JSON.parse(JSON.stringify(layout)),
        label,
        createdAt: Date.now(),
    };
}

function AdminPreviewDetailModal({ card, open, onClose }) {
    if (!open) {
        return null;
    }

    const isBuiltInModal = card.actionType === 'modal' && Boolean(card.actionValue);

    const title = card.detailTitle || card.title || 'Preview';
    const summary = card.descriptionHtml || '<p>No card description yet.</p>';
    const content = card.detailContentHtml || summary;
    const parsedLayout = parseLayoutJson(card.layoutJson, card.section);
    const imageUrl = resolveContentAssetUrl(card.detailImageUrl || parsedLayout?.detail?.imageUrl || card.imageUrl);

    return (
        <div className="admin-preview-modal" role="dialog" aria-modal="true" aria-label="Card detail preview">
            <div className="admin-preview-modal__backdrop" onClick={onClose} />
            <div className={`admin-preview-modal__panel ${isBuiltInModal ? 'is-built-in-modal' : ''}`}>
                <button type="button" className="admin-preview-modal__close" onClick={onClose} aria-label="Close preview">
                    <FiX />
                </button>
                {isBuiltInModal ? (
                    <div className="admin-preview-modal__builtin">
                        <div className="admin-preview-modal__builtin-meta">
                            {card.badgeText ? <span className={`home-news-badge home-news-${card.badgeVariant || 'update'}`}>{card.badgeText}</span> : null}
                            <h2>{card.title || 'Built-in pop-up preview'}</h2>
                            <p>This card opens one of the predefined Home pop-ups. This preview shows which pop-up the user will see.</p>
                        </div>
                        <BuiltInModalScaledPreview modalKey={card.actionValue} />
                    </div>
                ) : (
                    <>
                        <div className="admin-preview-modal__hero">
                            {card.badgeText ? <span className={`home-news-badge home-news-${card.badgeVariant || 'update'}`}>{card.badgeText}</span> : null}
                            <h2>{title}</h2>
                            <div
                                className="admin-preview-modal__summary"
                                dangerouslySetInnerHTML={{ __html: DOMPurify.sanitize(summary) }}
                            />
                        </div>
                        {imageUrl ? <img src={imageUrl} alt={title} className="admin-preview-modal__image" /> : null}
                        <div
                            className="admin-preview-modal__body"
                            dangerouslySetInnerHTML={{ __html: DOMPurify.sanitize(content) }}
                        />
                    </>
                )}
            </div>
        </div>
    );
}

function BuiltInModalScaledPreview({ modalKey }) {
    const frameRef = useRef(null);
    const builtin = getHomePromoBuiltin(modalKey);
    const baseWidth = builtin?.previewWidth || 940;
    const [scale, setScale] = useState(1);

    useLayoutEffect(() => {
        if (!frameRef.current) {
            return undefined;
        }

        const node = frameRef.current;
        const updateScale = () => {
            const availableWidth = Math.max(node.clientWidth - 16, 240);
            const nextScale = Math.min(1, availableWidth / baseWidth);
            setScale(nextScale);
        };

        updateScale();
        const observer = new ResizeObserver(updateScale);
        observer.observe(node);

        return () => observer.disconnect();
    }, [baseWidth]);

    const scaledHeight = Math.max(360, Math.round(760 * scale));

    return (
        <div ref={frameRef} className="admin-studio-builtinPreviewFrame">
            <div className="admin-studio-builtinPreviewViewport" style={{ height: `${scaledHeight}px` }}>
                <div
                    className="admin-studio-builtinPreviewScaler"
                    style={{
                        width: `${baseWidth}px`,
                        transform: `scale(${scale})`,
                    }}
                >
                    <div className="admin-studio-builtinPreviewCanvas">
                        <HomePromoModal modalKey={modalKey} />
                    </div>
                </div>
            </div>
        </div>
    );
}

function AdminAccessPanel({
    admins,
    adminEmail,
    adminFeedback,
    adminLoading,
    adminSaving,
    onEmailChange,
    onCreate,
    onRefresh,
    onActivate,
    onDeactivate,
    onRemove,
}) {
    return (
        <div className="admin-access-panel">
            <div className="admin-access-panel__header">
                <div>
                    <p className="admin-content-kicker">Access Control</p>
                    <h2>Admin users</h2>
                    <span>Admins are resolved from the database role when they sign in with Microsoft Entra ID.</span>
                </div>
                <button type="button" className="admin-secondary-btn" onClick={onRefresh} disabled={adminLoading}>
                    <FiRefreshCw /> Refresh
                </button>
            </div>

            <form className="admin-access-create" onSubmit={onCreate}>
                <label htmlFor="admin-email">Create admin by email</label>
                <div className="admin-access-create__row">
                    <input
                        id="admin-email"
                        type="email"
                        value={adminEmail}
                        onChange={(event) => onEmailChange(event.target.value)}
                        placeholder="name@solvoglobal.com"
                        autoComplete="off"
                    />
                    <button type="submit" className="admin-primary-btn" disabled={adminSaving || !adminEmail.trim()}>
                        <FiUserPlus /> {adminSaving ? 'Saving...' : 'Add admin'}
                    </button>
                </div>
            </form>

            {adminFeedback ? <div className="admin-feedback-banner">{adminFeedback}</div> : null}

            <div className="admin-access-list">
                {adminLoading ? (
                    <div className="admin-empty-state">Loading admins...</div>
                ) : admins.length ? (
                    admins.map((admin) => (
                        <div key={admin.id} className={`admin-access-card ${admin.isActive ? '' : 'is-inactive'}`}>
                            <div className="admin-access-card__identity">
                                <strong>{admin.fullName || admin.email}</strong>
                                <span>{admin.email}</span>
                            </div>
                            <span className={`admin-access-card__status ${admin.isActive ? 'is-active' : 'is-inactive'}`}>
                                {admin.isActive ? 'Active admin' : 'Inactive admin'}
                            </span>
                            <div className="admin-access-card__actions">
                                {admin.isActive ? (
                                    <button type="button" className="admin-secondary-btn" onClick={() => onDeactivate(admin.id)} disabled={adminSaving}>
                                        Deactivate
                                    </button>
                                ) : (
                                    <button type="button" className="admin-secondary-btn" onClick={() => onActivate(admin.id)} disabled={adminSaving}>
                                        Activate
                                    </button>
                                )}
                                <button type="button" className="admin-secondary-btn admin-danger-btn" onClick={() => onRemove(admin.id)} disabled={adminSaving}>
                                    <FiTrash2 /> Remove
                                </button>
                            </div>
                        </div>
                    ))
                ) : (
                    <div className="admin-empty-state">No admin users found.</div>
                )}
            </div>
        </div>
    );
}

export default function AdminContentPage() {
    const pageRef = useRef(null);
    const sidebarShellRef = useRef(null);
    const sidebarPanelRef = useRef(null);
    const sidebarToggleRef = useRef(null);
    const sidebarSectionRefs = useRef({});
    const pendingHistoryRef = useRef(null);
    const historyQueueRef = useRef(null);
    const [cards, setCards] = useState([]);
    const [selectedId, setSelectedId] = useState(null);
    const [form, setForm] = useState(() => createEmptyCard());
    const [layout, setLayout] = useState(() => createDefaultLayout(CONTENT_SECTIONS.spotlight));
    const [selectedLayer, setSelectedLayer] = useState('title');
    const [selectedLayers, setSelectedLayers] = useState(['title']);
    const [isLoading, setIsLoading] = useState(true);
    const [isSaving, setIsSaving] = useState(false);
    const [isUploading, setIsUploading] = useState(false);
    const [isDirty, setIsDirty] = useState(false);
    const [feedback, setFeedback] = useState('');
    const [previewOpen, setPreviewOpen] = useState(false);
    const [historyPast, setHistoryPast] = useState([]);
    const [historyFuture, setHistoryFuture] = useState([]);
    const [sidebarCollapsed, setSidebarCollapsed] = useState(true);
    const [sidebarQuery, setSidebarQuery] = useState('');
    const [sectionCollapsed, setSectionCollapsed] = useState({
        [CONTENT_SECTIONS.spotlight]: false,
        [CONTENT_SECTIONS.programNews]: false,
    });
    const [inspectorCollapsed, setInspectorCollapsed] = useState(false);
    const [canvasViewMode, setCanvasViewMode] = useState('card');
    const [workspaceTool, setWorkspaceTool] = useState('cards');
    const [inspectorMode, setInspectorMode] = useState('text');
    const [panelSection, setPanelSection] = useState(DEFAULT_PANEL_SECTION.text);
    const [draggedLayerId, setDraggedLayerId] = useState(null);
    const [layerDropTarget, setLayerDropTarget] = useState(null);
    const [assetLibrary, setAssetLibrary] = useState([]);
    const [mediaDropActive, setMediaDropActive] = useState(false);
    const [admins, setAdmins] = useState([]);
    const [adminEmail, setAdminEmail] = useState('');
    const [adminFeedback, setAdminFeedback] = useState('');
    const [adminLoading, setAdminLoading] = useState(false);
    const [adminSaving, setAdminSaving] = useState(false);
    const sideBarContext = useSideBar();
    const isMobileAdminBlocked = Boolean(sideBarContext?.isMobile);
    const previousGlobalSidebarOpenRef = useRef(null);

    usePageMotion(pageRef, []);

    useEffect(() => {
        if (!sideBarContext?.setOpen) {
            return undefined;
        }

        previousGlobalSidebarOpenRef.current = sideBarContext.open;
        sideBarContext.setOpen(false);

        return () => {
            if (previousGlobalSidebarOpenRef.current !== null) {
                sideBarContext.setOpen(previousGlobalSidebarOpenRef.current);
            }
        };
    }, [sideBarContext?.setOpen]);

    useEffect(() => {
        if (typeof window === 'undefined') {
            return;
        }

        try {
            const raw = window.localStorage.getItem(ASSET_LIBRARY_STORAGE_KEY);
            if (!raw) {
                return;
            }

            const parsed = JSON.parse(raw);
            if (Array.isArray(parsed)) {
                setAssetLibrary(parsed);
            }
        } catch {
            setAssetLibrary([]);
        }
    }, []);

    useEffect(() => {
        if (typeof window === 'undefined') {
            return;
        }

        window.localStorage.setItem(ASSET_LIBRARY_STORAGE_KEY, JSON.stringify(assetLibrary));
    }, [assetLibrary]);

    useLayoutEffect(() => {
        if (!sidebarShellRef.current || !sidebarPanelRef.current || !sidebarToggleRef.current) {
            return undefined;
        }

        const shell = sidebarShellRef.current;
        const panel = sidebarPanelRef.current;
        const toggle = sidebarToggleRef.current;
        const expandedWidth = 310;
        const collapsedWidth = 86;

        gsap.killTweensOf([shell, panel, toggle]);
        gsap.set([shell, panel, toggle], { willChange: 'transform, opacity, width' });

        if (sidebarCollapsed) {
            gsap.to(panel, {
                x: -18,
                opacity: 0,
                duration: 0.2,
                ease: 'power2.in',
                pointerEvents: 'none',
            });
            gsap.to(shell, {
                width: collapsedWidth,
                minWidth: collapsedWidth,
                opacity: 1,
                duration: 0.26,
                ease: 'power3.inOut',
            });
            gsap.to(toggle, {
                x: 0,
                duration: 0.24,
                ease: 'power3.out',
            });
        } else {
            gsap.set(shell, { overflow: 'hidden' });
            gsap.fromTo(
                shell,
                { width: shell.offsetWidth || collapsedWidth, minWidth: shell.offsetWidth || collapsedWidth, opacity: 1 },
                {
                    width: expandedWidth,
                    minWidth: expandedWidth,
                    opacity: 1,
                    duration: 0.34,
                    ease: 'power3.out',
                },
            );
            gsap.fromTo(
                panel,
                { x: -12, opacity: 0 },
                { x: 0, opacity: 1, pointerEvents: 'auto', duration: 0.26, ease: 'power3.out', delay: 0.03 },
            );
            gsap.fromTo(
                toggle,
                { x: -8 },
                { x: 0, duration: 0.3, ease: 'power3.out' },
            );
        }

        return () => {
            gsap.set([shell, panel, toggle], { clearProps: 'willChange' });
            gsap.killTweensOf([shell, panel, toggle]);
        };
    }, [sidebarCollapsed]);

    useLayoutEffect(() => {
        Object.entries(sidebarSectionRefs.current).forEach(([section, node]) => {
            if (!node) {
                return;
            }

            gsap.killTweensOf(node);

            if (sectionCollapsed[section]) {
                gsap.to(node, {
                    height: 0,
                    opacity: 0,
                    y: -8,
                    duration: 0.22,
                    ease: 'power2.out',
                    overflow: 'hidden',
                });
                return;
            }

            gsap.set(node, { overflow: 'hidden' });
            gsap.to(node, {
                height: 'auto',
                opacity: 1,
                y: 0,
                duration: 0.28,
                ease: 'power2.out',
            });
        });

        return () => {
            Object.values(sidebarSectionRefs.current).forEach((node) => {
                if (node) {
                    gsap.killTweensOf(node);
                }
            });
        };
    }, [sectionCollapsed, cards]);

    const cardsBySection = useMemo(
        () => ({
            [CONTENT_SECTIONS.spotlight]: cards.filter((card) => card.section === CONTENT_SECTIONS.spotlight),
            [CONTENT_SECTIONS.programNews]: cards.filter((card) => card.section === CONTENT_SECTIONS.programNews),
        }),
        [cards],
    );

    const totalPublished = useMemo(() => cards.filter((card) => card.isPublished).length, [cards]);
    const normalizedSidebarQuery = sidebarQuery.trim().toLowerCase();
    const filteredCardsBySection = useMemo(
        () =>
            Object.fromEntries(
                Object.entries(cardsBySection).map(([section, sectionCards]) => [
                    section,
                    normalizedSidebarQuery
                        ? sectionCards.filter((card) => {
                              const haystack = [card.title, card.badgeText, card.buttonText, card.dateText]
                                  .filter(Boolean)
                                  .join(' ')
                                  .toLowerCase();
                              return haystack.includes(normalizedSidebarQuery);
                          })
                        : sectionCards,
                ]),
            ),
        [cardsBySection, normalizedSidebarQuery],
    );
    const activeLayoutTarget = useMemo(
        () => (canvasViewMode === 'detail' ? layout.detail : layout),
        [canvasViewMode, layout],
    );
    const studioLayers = useMemo(() => {
        const baseDefinitions = canvasViewMode === 'detail'
            ? [
                  { key: 'image', label: 'Pop-up image' },
                  { key: 'title', label: 'Pop-up title' },
                  { key: 'body', label: 'Pop-up body' },
              ]
            : CARD_STUDIO_LAYERS;

        return getOrderedLayerEntries(activeLayoutTarget, baseDefinitions);
    }, [activeLayoutTarget, canvasViewMode]);
    const selectionIds = useMemo(
        () => (selectedLayers.length ? selectedLayers : selectedLayer ? [selectedLayer] : []),
        [selectedLayer, selectedLayers],
    );

    function clearQueuedHistory() {
        if (historyQueueRef.current) {
            window.clearTimeout(historyQueueRef.current);
            historyQueueRef.current = null;
        }
        pendingHistoryRef.current = null;
    }

    function commitHistorySnapshot(snapshot, { clearFuture = true } = {}) {
        if (!snapshot) {
            return;
        }

        setHistoryPast((current) => {
            const last = current[current.length - 1];
            if (last && JSON.stringify(last.form) === JSON.stringify(snapshot.form) && JSON.stringify(last.layout) === JSON.stringify(snapshot.layout)) {
                return current;
            }
            return [...current, snapshot].slice(-100);
        });

        if (clearFuture) {
            setHistoryFuture([]);
        }
    }

    function flushQueuedHistory() {
        if (!pendingHistoryRef.current) {
            return;
        }

        commitHistorySnapshot(pendingHistoryRef.current);
        clearQueuedHistory();
    }

    async function loadCards() {
        clearQueuedHistory();
        setIsLoading(true);
        setFeedback('');
        try {
            const response = await fetchContentCards(undefined, true);
            const items = sortCards(Array.isArray(response?.data) ? response.data : []);
            setCards(items);

            if (items.length === 0) {
                const empty = createEmptyCard(CONTENT_SECTIONS.spotlight);
                setSelectedId(null);
                setForm(empty);
                setLayout(createDefaultLayout(empty.section));
                setSelectedLayer('title');
                setSelectedLayers(['title']);
                setCanvasViewMode('card');
            } else {
                const nextSelected = items.find((item) => item.id === selectedId) || items[0];
                const normalized = normalizeCard(nextSelected, nextSelected.section || CONTENT_SECTIONS.spotlight);
                setSelectedId(nextSelected.id);
                setForm(normalized);
                setLayout(parseLayoutJson(normalized.layoutJson, normalized.section));
                setSelectedLayer('title');
                setSelectedLayers(['title']);
                setCanvasViewMode('card');
            }
            setIsDirty(false);
            setHistoryPast([]);
            setHistoryFuture([]);
        } catch (error) {
            setFeedback('Could not load content cards.');
            setCards([]);
        } finally {
            setIsLoading(false);
        }
    }

    async function loadAdmins() {
        setAdminLoading(true);
        setAdminFeedback('');
        try {
            const response = await fetchAdminUsers();
            if (!response?.success) {
                setAdminFeedback(response?.errors?.[0] || 'Could not load admin users.');
                return;
            }

            setAdmins(Array.isArray(response.data) ? response.data : []);
        } catch {
            setAdminFeedback('Could not load admin users.');
        } finally {
            setAdminLoading(false);
        }
    }

    useEffect(() => {
        loadCards();
    }, []);

    useEffect(() => {
        if (workspaceTool === 'admins') {
            loadAdmins();
        }
    }, [workspaceTool]);

    useEffect(() => () => {
        clearQueuedHistory();
    }, []);

    function markDirty() {
        setIsDirty(true);
    }

    async function handleCreateAdmin(event) {
        event.preventDefault();
        const email = adminEmail.trim();
        if (!email) {
            return;
        }

        setAdminSaving(true);
        setAdminFeedback('');
        try {
            const response = await createAdminUser(email);
            if (!response?.success) {
                setAdminFeedback(response?.errors?.[0] || 'Could not create admin user.');
                return;
            }

            setAdminEmail('');
            setAdminFeedback('Admin access saved.');
            await loadAdmins();
        } catch {
            setAdminFeedback('Could not create admin user.');
        } finally {
            setAdminSaving(false);
        }
    }

    async function handleAdminAction(action, successMessage) {
        setAdminSaving(true);
        setAdminFeedback('');
        try {
            const response = await action();
            if (!response?.success) {
                setAdminFeedback(response?.errors?.[0] || 'Could not update admin access.');
                return;
            }

            setAdminFeedback(successMessage);
            await loadAdmins();
        } catch {
            setAdminFeedback('Could not update admin access.');
        } finally {
            setAdminSaving(false);
        }
    }

    function handleActivateAdmin(id) {
        handleAdminAction(() => activateAdminUser(id), 'Admin activated.');
    }

    function handleDeactivateAdmin(id) {
        handleAdminAction(() => deactivateAdminUser(id), 'Admin deactivated.');
    }

    function handleRemoveAdmin(id) {
        if (!window.confirm('Remove admin access for this user?')) {
            return;
        }

        handleAdminAction(() => removeAdminUser(id), 'Admin access removed.');
    }

    function pushHistoryNow(labelOrForm = form, maybeForm = layout, maybeLayout = layout) {
        flushQueuedHistory();
        const label = typeof labelOrForm === 'string' ? labelOrForm : 'Canvas edit';
        const nextForm = typeof labelOrForm === 'string' ? maybeForm : labelOrForm;
        const nextLayout = typeof labelOrForm === 'string' ? maybeLayout : maybeForm;
        commitHistorySnapshot(cloneSnapshot(nextForm, nextLayout, label));
    }

    function queueHistory(labelOrForm = form, maybeForm = layout, maybeLayout = layout) {
        const label = typeof labelOrForm === 'string' ? labelOrForm : 'Canvas edit';
        const nextForm = typeof labelOrForm === 'string' ? maybeForm : labelOrForm;
        const nextLayout = typeof labelOrForm === 'string' ? maybeLayout : maybeForm;

        if (!pendingHistoryRef.current) {
            pendingHistoryRef.current = cloneSnapshot(nextForm, nextLayout, label);
        }

        if (historyQueueRef.current) {
            return;
        }

        historyQueueRef.current = window.setTimeout(() => {
            commitHistorySnapshot(pendingHistoryRef.current);
            clearQueuedHistory();
        }, 260);
    }

    function handleSelectCard(card) {
        const normalized = normalizeCard(card, card.section || CONTENT_SECTIONS.spotlight);
        setSelectedId(card.id);
        setForm(normalized);
        setLayout(parseLayoutJson(normalized.layoutJson, normalized.section));
        setSelectedLayer('title');
        setSelectedLayers(['title']);
        setCanvasViewMode('card');
        setFeedback('');
        setIsDirty(false);
        setHistoryPast([]);
        setHistoryFuture([]);
    }

    function handleCreateNew(section = CONTENT_SECTIONS.spotlight) {
        const sourceCards = cards.filter((item) => item.section === section);
        const nextOrder = sourceCards.length ? Math.max(...sourceCards.map((item) => item.displayOrder || 0)) + 1 : 1;
        const next = {
            ...createEmptyCard(section),
            displayOrder: nextOrder,
        };
        setSelectedId(null);
        setForm(next);
        setLayout(createDefaultLayout(section));
        setSelectedLayer('title');
        setSelectedLayers(['title']);
        setCanvasViewMode('card');
        setFeedback('Creating a new card. Use the canvas to shape it, then save.');
        setIsDirty(true);
        setHistoryPast([]);
        setHistoryFuture([]);
    }

    function updateField(field, value) {
        queueHistory(`Edit ${field}`, form, layout);
        setForm((current) => {
            let base = current;
            if ((field === 'detailTitle' || field === 'detailContentHtml') && current.actionType === 'modal' && current.actionValue) {
                const editableCopy = createEditableDetailFromBuiltin(current.actionValue, current.title);
                base = {
                    ...current,
                    actionType: 'detail',
                    actionValue: '',
                    detailTitle: current.detailTitle || editableCopy.detailTitle,
                    detailContentHtml: current.detailContentHtml || editableCopy.detailContentHtml,
                };
            }

            const next = { ...base, [field]: value };
            if (field === 'section' && value !== current.section) {
                setLayout(createDefaultLayout(value));
                setSelectedLayer('title');
                setSelectedLayers(['title']);
                setCanvasViewMode('card');
            }
            if (field === 'actionType' && !['detail', 'modal'].includes(value) && canvasViewMode === 'detail') {
                setCanvasViewMode('card');
            }
            if ((field === 'detailTitle' || field === 'detailContentHtml') && canvasViewMode !== 'detail') {
                setCanvasViewMode('detail');
            }
            return next;
        });
        markDirty();
    }

    function handleInlineTextChange(layerId, value) {
        const nextValue = value ?? '';
        const customLayer = activeLayoutTarget.customElements?.find((item) => item.id === layerId);

        if (customLayer?.sourceKey === 'freeText') {
            updateLayoutElement(layerId, { text: nextValue });
            return;
        }

        switch (layerId) {
            case 'badge':
                updateField('badgeText', nextValue);
                break;
            case 'title':
                updateField(canvasViewMode === 'detail' ? 'detailTitle' : 'title', nextValue);
                break;
            case 'summary':
                updateField('descriptionHtml', `<p>${nextValue.replace(/\n/g, '<br />')}</p>`);
                break;
            case 'body':
                updateField('detailContentHtml', `<p>${nextValue.replace(/\n/g, '<br />')}</p>`);
                break;
            case 'date':
                updateField('dateText', nextValue);
                break;
            case 'button':
                updateField('buttonText', nextValue);
                break;
            default:
                break;
        }
    }

    function handleAddTextLayer(preset = 'body') {
        const now = Date.now();
        const isDetail = canvasViewMode === 'detail';
        const frame = isDetail ? layout.detail.modal : layout.card;
        const textPresets = {
            headline: {
                text: 'Add a headline',
                fontSize: isDetail ? 34 : 30,
                fontStyle: 'bold',
                width: Math.min(520, Math.max(260, frame.width - 120)),
                height: 58,
                fill: '#0B2135',
            },
            body: {
                text: 'Add supporting text',
                fontSize: isDetail ? 21 : 18,
                fontStyle: 'normal',
                width: Math.min(560, Math.max(280, frame.width - 120)),
                height: 84,
                fill: '#4F6277',
            },
            label: {
                text: 'Small label',
                fontSize: 15,
                fontStyle: 'bold',
                width: 220,
                height: 34,
                fill: '#0B7382',
            },
        };
        const chosen = textPresets[preset] || textPresets.body;
        const nextLayerId = `free-text-${now}`;

        mutateActiveLayout('Add text layer', (target) => {
            const existing = [
                ...Object.values(target.elements || {}),
                ...(target.customElements || []),
            ];
            const maxZ = existing.reduce((max, item, index) => Math.max(max, Number.isFinite(item.zIndex) ? item.zIndex : index), 0);

            return {
                ...target,
                customElements: [
                    ...(target.customElements || []),
                    {
                        id: nextLayerId,
                        sourceKey: 'freeText',
                        x: Math.round(frame.x + 72),
                        y: Math.round(frame.y + 92 + ((target.customElements || []).length * 24)),
                        width: chosen.width,
                        height: chosen.height,
                        text: chosen.text,
                        fontFamily: 'Poppins',
                        fontStyle: chosen.fontStyle,
                        fontSize: chosen.fontSize,
                        lineHeight: preset === 'label' ? 1.1 : 1.35,
                        fill: chosen.fill,
                        visible: true,
                        zIndex: maxZ + 10,
                    },
                ],
            };
        }, 'Text layer added.');
        setSelectedLayer(nextLayerId);
        setSelectedLayers([nextLayerId]);
        setInspectorMode('text');
    }

    function mutateActiveLayout(label, mutator, feedbackMessage = '') {
        pushHistoryNow(label, form, layout);
        setLayout((current) => {
            const target = canvasViewMode === 'detail' ? current.detail : current;
            const nextTarget = mutator(target);
            return canvasViewMode === 'detail'
                ? { ...current, detail: nextTarget }
                : nextTarget;
        });
        if (feedbackMessage) {
            setFeedback(feedbackMessage);
        }
        markDirty();
    }

    function mapSelection(target, updater) {
        return {
            ...target,
            elements: Object.fromEntries(
                Object.entries(target.elements || {}).map(([key, value]) => [
                    key,
                    selectionIds.includes(key) ? updater(value, key) : value,
                ]),
            ),
            customElements: (target.customElements || []).map((item) =>
                selectionIds.includes(item.id) ? updater(item, item.id) : item,
            ),
        };
    }

    function getSelectedCanvasItems(target = activeLayoutTarget) {
        return selectionIds
            .map((id) => target.elements?.[id] || target.customElements?.find((item) => item.id === id))
            .filter(Boolean);
    }

    function updateLayoutElement(elementKey, patch) {
        queueHistory(`Update ${elementKey}`, form, layout);
        if (canvasViewMode === 'detail' && form.actionType === 'modal' && form.actionValue) {
            const editableCopy = createEditableDetailFromBuiltin(form.actionValue, form.title);
            setForm((current) => ({
                ...current,
                actionType: 'detail',
                actionValue: '',
                detailTitle: current.detailTitle || editableCopy.detailTitle,
                detailContentHtml: current.detailContentHtml || editableCopy.detailContentHtml,
            }));
        }
        setLayout((current) => {
            const target = canvasViewMode === 'detail' ? current.detail : current;
            const nextTarget = {
                ...target,
                elements: target.elements[elementKey]
                    ? {
                          ...target.elements,
                          [elementKey]: {
                              ...target.elements[elementKey],
                              ...patch,
                          },
                      }
                    : target.elements,
                customElements: target.elements[elementKey]
                    ? target.customElements
                    : (target.customElements || []).map((item) =>
                          item.id === elementKey
                              ? {
                                    ...item,
                                    ...patch,
                                }
                              : item,
                      ),
            };

            return canvasViewMode === 'detail'
                ? {
                      ...current,
                      detail: nextTarget,
                  }
                : nextTarget;
        });
        markDirty();
    }

    function updateCardSurface(patch) {
        queueHistory('Update card surface', form, layout);
        if (canvasViewMode === 'detail' && form.actionType === 'modal' && form.actionValue) {
            const editableCopy = createEditableDetailFromBuiltin(form.actionValue, form.title);
            setForm((current) => ({
                ...current,
                actionType: 'detail',
                actionValue: '',
                detailTitle: current.detailTitle || editableCopy.detailTitle,
                detailContentHtml: current.detailContentHtml || editableCopy.detailContentHtml,
            }));
        }
        setLayout((current) => {
            if (canvasViewMode === 'detail') {
                return {
                    ...current,
                    detail: {
                        ...current.detail,
                        modal: {
                            ...current.detail.modal,
                            ...patch,
                        },
                    },
                };
            }

            return {
                ...current,
                card: {
                    ...current.card,
                    ...patch,
                },
            };
        });
        markDirty();
    }

    function updateDetailImageUrl(value) {
        const normalizedUrl = normalizeImageUrl(value);
        queueHistory('Update pop-up image', form, layout);
        setLayout((current) => ({
            ...current,
            detail: {
                ...current.detail,
                imageUrl: normalizedUrl,
            },
        }));
        markDirty();
    }

    function applyImageUrlForScope(normalizedUrl, scope = 'card', iconKey) {
        if (scope === 'detail') {
            setLayout((current) => ({
                ...current,
                detail: {
                    ...current.detail,
                    imageUrl: normalizedUrl,
                },
            }));
            return;
        }

        setForm((current) => ({
            ...current,
            imageUrl: normalizedUrl,
            iconKey: iconKey ?? current.iconKey,
        }));
    }

    function handleLayerSelection(layerId, options = {}) {
        const { append = false } = options;

        if (!layerId) {
            setSelectedLayer(null);
            setSelectedLayers([]);
            return;
        }

        if (!append) {
            setSelectedLayer(layerId);
            setSelectedLayers([layerId]);
            return;
        }

        setSelectedLayer(layerId);
        setSelectedLayers((current) => {
            const exists = current.includes(layerId);
            if (exists) {
                const next = current.filter((item) => item !== layerId);
                return next.length ? next : [layerId];
            }
            return [...current, layerId];
        });
    }

    function handleDuplicateLayer() {
        if (!selectionIds.length) {
            return;
        }

        const activeLayout = canvasViewMode === 'detail' ? layout.detail : layout;
        const sources = selectionIds
            .map((layerId) => activeLayout.elements[layerId] || activeLayout.customElements?.find((item) => item.id === layerId))
            .filter(Boolean);

        if (!sources.length) {
            return;
        }

        const nextSelection = [];

        pushHistoryNow('Duplicate layers', form, layout);
        setLayout((current) => {
            const target = canvasViewMode === 'detail' ? current.detail : current;
            const nextTarget = {
                ...target,
                customElements: [
                    ...(target.customElements || []),
                    ...sources.map((source, index) => {
                        const sourceKey = source.sourceKey || source.id;
                        const duplicateId = `${sourceKey}-copy-${Date.now()}-${index}`;
                        nextSelection.push(duplicateId);
                        return {
                            ...source,
                            id: duplicateId,
                            sourceKey,
                            x: (source.x || 0) + 28,
                            y: (source.y || 0) + 28,
                            zIndex: (Number.isFinite(source.zIndex) ? source.zIndex : 0) + 10,
                        };
                    }),
                ],
            };

            return canvasViewMode === 'detail'
                ? { ...current, detail: nextTarget }
                : nextTarget;
        });
        setSelectedLayer(nextSelection[nextSelection.length - 1] || null);
        setSelectedLayers(nextSelection);
        setFeedback(nextSelection.length > 1 ? 'Layers duplicated on the canvas.' : 'Layer duplicated on the canvas.');
        markDirty();
    }

    function handleDeleteLayer() {
        if (!selectionIds.length) {
            return;
        }

        pushHistoryNow('Delete layers', form, layout);
        setLayout((current) => {
            const target = canvasViewMode === 'detail' ? current.detail : current;
            const nextTarget = {
                ...target,
                elements: Object.fromEntries(
                    Object.entries(target.elements || {}).map(([key, value]) => [
                        key,
                        selectionIds.includes(key)
                            ? {
                                  ...value,
                                  visible: false,
                              }
                            : value,
                    ]),
                ),
                customElements: (target.customElements || []).filter((item) => !selectionIds.includes(item.id)),
            };
            return canvasViewMode === 'detail'
                ? { ...current, detail: nextTarget }
                : nextTarget;
        });
        setSelectedLayer('title');
        setSelectedLayers(['title']);
        setFeedback(selectionIds.length > 1 ? 'Layers removed from the canvas.' : 'Layer removed from the canvas.');
        markDirty();
    }

    function handleToggleLayerVisibility(layerId) {
        const activeLayout = canvasViewMode === 'detail' ? layout.detail : layout;
        const baseLayer = activeLayout.elements[layerId];
        const customLayer = activeLayout.customElements?.find((item) => item.id === layerId);
        if (!baseLayer && !customLayer) {
            return;
        }

        pushHistoryNow('Toggle visibility', form, layout);

        if (baseLayer) {
            setLayout((current) => {
                const target = canvasViewMode === 'detail' ? current.detail : current;
                const nextTarget = {
                    ...target,
                    elements: {
                        ...target.elements,
                        [layerId]: {
                            ...target.elements[layerId],
                            visible: target.elements[layerId].visible === false,
                        },
                    },
                };
                return canvasViewMode === 'detail'
                    ? { ...current, detail: nextTarget }
                    : nextTarget;
            });
        } else {
            setLayout((current) => {
                const target = canvasViewMode === 'detail' ? current.detail : current;
                const nextTarget = {
                    ...target,
                    customElements: (target.customElements || []).map((item) =>
                        item.id === layerId
                            ? {
                                  ...item,
                                  visible: item.visible === false,
                              }
                            : item,
                    ),
                };
                return canvasViewMode === 'detail'
                    ? { ...current, detail: nextTarget }
                    : nextTarget;
            });
        }

        markDirty();
    }

    function handleAlignSelection(mode) {
        const items = getSelectedCanvasItems();
        if (!items.length) {
            return;
        }

        const frame = canvasViewMode === 'detail' ? layout.detail.modal : layout.card;
        mutateActiveLayout('Align layers', (target) =>
            mapSelection(target, (item) => {
                const width = item.width || 0;
                const height = item.height || 0;

                switch (mode) {
                    case 'left':
                        return { ...item, x: frame.x };
                    case 'center':
                        return { ...item, x: Math.round(frame.x + ((frame.width - width) / 2)) };
                    case 'right':
                        return { ...item, x: Math.round(frame.x + frame.width - width) };
                    case 'top':
                        return { ...item, y: frame.y };
                    case 'middle':
                        return { ...item, y: Math.round(frame.y + ((frame.height - height) / 2)) };
                    case 'bottom':
                        return { ...item, y: Math.round(frame.y + frame.height - height) };
                    default:
                        return item;
                }
            }),
        );
    }

    function handleDistributeSelection(axis) {
        const items = getSelectedCanvasItems()
            .map((item, index) => ({ item, index }))
            .sort((left, right) => (axis === 'horizontal' ? left.item.x - right.item.x : left.item.y - right.item.y));

        if (items.length < 3) {
            return;
        }

        const first = items[0].item;
        const last = items[items.length - 1].item;
        const totalSize = items.reduce((sum, entry) => sum + (axis === 'horizontal' ? entry.item.width || 0 : entry.item.height || 0), 0);
        const start = axis === 'horizontal' ? first.x : first.y;
        const end = axis === 'horizontal'
            ? (last.x + (last.width || 0))
            : (last.y + (last.height || 0));
        const gap = (end - start - totalSize) / (items.length - 1);
        let cursor = start;

        const nextPositionMap = new Map();
        items.forEach(({ item }) => {
            nextPositionMap.set(item.id, axis === 'horizontal' ? { x: Math.round(cursor) } : { y: Math.round(cursor) });
            cursor += (axis === 'horizontal' ? item.width || 0 : item.height || 0) + gap;
        });

        mutateActiveLayout('Distribute layers', (target) =>
            mapSelection(target, (item, itemId) => ({
                ...item,
                ...(nextPositionMap.get(itemId) || {}),
            })),
        );
    }

    function handleArrangeLayers(ids, mode) {
        const allItems = [
            ...Object.entries(activeLayoutTarget.elements || {}).map(([id, item], index) => ({
                id,
                zIndex: Number.isFinite(item.zIndex) ? item.zIndex : index,
            })),
            ...(activeLayoutTarget.customElements || []).map((item, index) => ({
                id: item.id,
                zIndex: Number.isFinite(item.zIndex) ? item.zIndex : 100 + index,
            })),
        ];

        if (!ids.length || !allItems.length) {
            return;
        }

        const maxZ = Math.max(...allItems.map((item) => item.zIndex));
        const minZ = Math.min(...allItems.map((item) => item.zIndex));

        mutateActiveLayout('Arrange layers', (target) => ({
            ...target,
            elements: Object.fromEntries(
                Object.entries(target.elements || {}).map(([key, value]) => {
                    if (!ids.includes(key)) {
                        return [key, value];
                    }

                    const currentZ = Number.isFinite(value.zIndex) ? value.zIndex : 0;
                    switch (mode) {
                        case 'forward':
                            return [key, { ...value, zIndex: currentZ + 10 }];
                        case 'backward':
                            return [key, { ...value, zIndex: currentZ - 10 }];
                        case 'front':
                            return [key, { ...value, zIndex: maxZ + 10 }];
                        case 'back':
                            return [key, { ...value, zIndex: minZ - 10 }];
                        default:
                            return [key, value];
                    }
                }),
            ),
            customElements: (target.customElements || []).map((item) => {
                if (!ids.includes(item.id)) {
                    return item;
                }

                const currentZ = Number.isFinite(item.zIndex) ? item.zIndex : 0;
                switch (mode) {
                    case 'forward':
                        return { ...item, zIndex: currentZ + 10 };
                    case 'backward':
                        return { ...item, zIndex: currentZ - 10 };
                    case 'front':
                        return { ...item, zIndex: maxZ + 10 };
                    case 'back':
                        return { ...item, zIndex: minZ - 10 };
                    default:
                        return item;
                }
            }),
        }));
    }

    function handleReorderLayer(draggedId, targetId, placement = 'before') {
        if (placement === 'start') {
            setDraggedLayerId(draggedId);
            return;
        }

        if (placement === 'cancel') {
            setDraggedLayerId(null);
            setLayerDropTarget(null);
            return;
        }

        if (placement === 'hover') {
            if (draggedId && targetId && draggedId !== targetId) {
                setLayerDropTarget(targetId);
            }
            return;
        }

        if (!draggedId || !targetId || draggedId === targetId) {
            setDraggedLayerId(null);
            setLayerDropTarget(null);
            return;
        }

        const orderedIds = studioLayers.map((layer) => layer.id);
        const draggedIndex = orderedIds.indexOf(draggedId);
        const targetIndex = orderedIds.indexOf(targetId);

        if (draggedIndex === -1 || targetIndex === -1) {
            setDraggedLayerId(null);
            setLayerDropTarget(null);
            return;
        }

        const nextIds = [...orderedIds];
        nextIds.splice(draggedIndex, 1);
        const insertionIndex = placement === 'after'
            ? targetIndex + (draggedIndex < targetIndex ? 0 : 1)
            : targetIndex + (draggedIndex < targetIndex ? -1 : 0);
        nextIds.splice(Math.max(0, insertionIndex), 0, draggedId);

        mutateActiveLayout('Reorder layers', (target) => resequenceLayerStack(target, nextIds), 'Layer order updated.');
        setDraggedLayerId(null);
        setLayerDropTarget(null);
    }

    function handleApplyImageFramePreset(preset) {
        const imageLayerId = selectionIds.find((id) => (activeLayoutTarget.elements?.[id] || activeLayoutTarget.customElements?.find((item) => item.id === id))?.sourceKey === 'image')
            || (selectedLayer === 'image' ? 'image' : selectionIds.find((id) => {
                const layer = activeLayoutTarget.elements?.[id] || activeLayoutTarget.customElements?.find((item) => item.id === id);
                return layer?.sourceKey === 'image';
            }))
            || ((activeLayoutTarget.elements?.image || null) ? 'image' : null);

        if (!imageLayerId) {
            return;
        }

        const imageLayer = activeLayoutTarget.elements?.[imageLayerId] || activeLayoutTarget.customElements?.find((item) => item.id === imageLayerId);
        if (!imageLayer) {
            return;
        }

        const layerWidth = imageLayer.width || 200;
        const layerHeight = imageLayer.height || 120;
        const patchByPreset = {
            fill: { imageFit: 'cover' },
            contain: { imageFit: 'contain' },
            reset: {
                cropX: null,
                cropY: null,
                cropWidth: null,
                cropHeight: null,
                imageOffsetX: 0,
                imageOffsetY: 0,
                imageScaleX: 1,
                imageScaleY: 1,
                imageRotation: 0,
                imageFit: 'cover',
            },
            square: { width: layerWidth, height: layerWidth, radius: 24, imageFit: 'cover' },
            portrait: { width: Math.round(layerHeight * 0.78), height: Math.round(layerHeight * 1.1), radius: 24, imageFit: 'cover' },
            soft: { radius: 32 },
            round: { radius: 999 },
        };

        const patch = patchByPreset[preset];
        if (!patch) {
            return;
        }

        updateLayoutElement(imageLayerId, patch);
        setFeedback('Image framing updated.');
    }

    function handleNudgeImageCrop(deltaX = 0, deltaY = 0, scaleDelta = 0) {
        const imageLayerId = selectionIds.find((id) => {
            const layer = activeLayoutTarget.elements?.[id] || activeLayoutTarget.customElements?.find((item) => item.id === id);
            return (layer?.sourceKey || id) === 'image';
        }) || (activeLayoutTarget.elements?.image ? 'image' : null);

        if (!imageLayerId) {
            return;
        }

        const imageLayer = activeLayoutTarget.elements?.[imageLayerId] || activeLayoutTarget.customElements?.find((item) => item.id === imageLayerId);
        if (!imageLayer) {
            return;
        }

        const nextCropWidth = imageLayer.cropWidth
            ? Math.max(40, imageLayer.cropWidth - scaleDelta)
            : imageLayer.cropWidth;
        const nextCropHeight = imageLayer.cropHeight
            ? Math.max(40, imageLayer.cropHeight - scaleDelta)
            : imageLayer.cropHeight;

        updateLayoutElement(imageLayerId, {
            cropX: Math.max(0, (imageLayer.cropX || 0) + deltaX),
            cropY: Math.max(0, (imageLayer.cropY || 0) + deltaY),
            cropWidth: Number.isFinite(nextCropWidth) ? nextCropWidth : imageLayer.cropWidth,
            cropHeight: Number.isFinite(nextCropHeight) ? nextCropHeight : imageLayer.cropHeight,
        });
    }

    function handleArrangeSelection(mode) {
        if (!selectionIds.length) {
            return;
        }
        handleArrangeLayers(selectionIds, mode);
    }

    function handleApplyTypographyPreset(preset) {
        const patch = {
            fontFamily: preset.fontFamily,
            fontSize: preset.fontSize,
            lineHeight: preset.lineHeight,
            fill: preset.fill,
        };

        mutateActiveLayout(`Apply ${preset.label} preset`, (target) => mapSelection(target, (item) => ({ ...item, ...patch })));
    }

    function handleApplyPalettePreset(palette) {
        const targetIds = selectionIds.length ? selectionIds : ['badge', 'title', 'summary', 'button'];

        mutateActiveLayout(`Apply ${palette.label} palette`, (target) => ({
            ...target,
            card: target.card
                ? {
                      ...target.card,
                      fill: palette.surface,
                      stroke: palette.surfaceStroke,
                      gradientStops: palette.gradientStops,
                  }
                : target.card,
            modal: target.modal
                ? {
                      ...target.modal,
                      fill: palette.surface,
                      stroke: palette.surfaceStroke,
                      gradientStops: palette.gradientStops,
                  }
                : target.modal,
            elements: Object.fromEntries(
                Object.entries(target.elements || {}).map(([key, value]) => {
                    if (!targetIds.includes(key)) {
                        return [key, value];
                    }

                    if (key === 'badge') {
                        return [key, { ...value, backgroundFill: palette.badgeFill, textColor: palette.badgeText, fill: palette.badgeText }];
                    }

                    if (key === 'button') {
                        return [key, { ...value, backgroundFill: palette.buttonFill, textColor: palette.buttonText, fill: palette.buttonText }];
                    }

                    if (key === 'summary' || key === 'body' || key === 'date') {
                        return [key, { ...value, fill: palette.support }];
                    }

                    return [key, { ...value, fill: palette.primary }];
                }),
            ),
            customElements: (target.customElements || []).map((item) => {
                if (selectionIds.length && !selectionIds.includes(item.id)) {
                    return item;
                }

                if (item.sourceKey === 'freeText') {
                    return { ...item, fill: palette.primary };
                }

                if (item.sourceKey === 'button') {
                    return { ...item, backgroundFill: palette.buttonFill, textColor: palette.buttonText, fill: palette.buttonText };
                }

                if (item.sourceKey === 'badge') {
                    return { ...item, backgroundFill: palette.badgeFill, textColor: palette.badgeText, fill: palette.badgeText };
                }

                return item;
            }),
        }), `${palette.label} palette applied.`);
    }

    function handleUndo() {
        flushQueuedHistory();
        if (!historyPast.length) {
            return;
        }

        const previous = historyPast[historyPast.length - 1];
        setHistoryPast((current) => current.slice(0, -1));
        setHistoryFuture((current) => [cloneSnapshot(form, layout), ...current].slice(0, 100));
        setForm(previous.form);
        setLayout(previous.layout);
        setIsDirty(true);
        setFeedback('Reverted the last change.');
    }

    function handleRedo() {
        flushQueuedHistory();
        if (!historyFuture.length) {
            return;
        }

        const [next, ...rest] = historyFuture;
        setHistoryFuture(rest);
        setHistoryPast((current) => [...current, cloneSnapshot(form, layout)].slice(-100));
        setForm(next.form);
        setLayout(next.layout);
        setIsDirty(true);
        setFeedback('Restored the next change.');
    }

    function handleResetTemplate() {
        pushHistoryNow('Reset template', form, layout);
        setLayout(createDefaultLayout(form.section));
        setSelectedLayer('title');
        setSelectedLayers(['title']);
        setFeedback('Layout reset to the default template for this section.');
        markDirty();
    }

    async function handleUpload(event, scope = canvasViewMode === 'detail' ? 'detail' : 'card') {
        const file = event?.target?.files?.[0] || event?.dataTransfer?.files?.[0];
        if (!file) {
            return;
        }

        setIsUploading(true);
        setFeedback(scope === 'detail' ? 'Uploading pop-up image...' : 'Uploading card image...');
        try {
            const response = await uploadContentImage(file);
            if (!response?.success || !response?.data?.url) {
                throw new Error(response?.errors?.[0] || 'Image upload failed.');
            }
            const normalizedUrl = normalizeImageUrl(response.data.url);
            pushHistoryNow(scope === 'detail' ? 'Upload pop-up image' : 'Upload card image', form, layout);
            applyImageUrlForScope(normalizedUrl, scope, scope === 'card' ? 'uploaded-image' : undefined);
            setAssetLibrary((current) =>
                mergePersistentAssets(current, [{
                    id: `upload-${Date.now()}`,
                    label: `${scope === 'detail' ? 'Pop-up' : 'Card'}: ${file.name || 'Uploaded image'}`,
                    type: 'image-url',
                    preview: normalizedUrl,
                    value: normalizedUrl,
                    iconKey: scope === 'card' ? 'uploaded-image' : undefined,
                    createdAt: Date.now(),
                }]),
            );
            setFeedback(scope === 'detail' ? 'Pop-up image uploaded. Save the card to persist it.' : 'Card image uploaded. Save the card to persist it.');
            markDirty();
        } catch (error) {
            setFeedback(error.message || 'Image upload failed.');
        } finally {
            setIsUploading(false);
            if (event?.target) {
                event.target.value = '';
            }
        }
    }

    function handleCanvasAssetDrop(nativeEvent) {
        const dataTransfer = nativeEvent?.dataTransfer;
        if (!dataTransfer) {
            return;
        }

        if (dataTransfer.files?.length) {
            handleUpload({ dataTransfer }, canvasViewMode === 'detail' ? 'detail' : 'card');
            return;
        }

        const payload = dataTransfer.getData('application/x-card-studio-asset');
        if (!payload) {
            return;
        }

        try {
            const parsed = JSON.parse(payload);
            if (parsed?.type === 'image-url' && parsed.value) {
                const normalizedUrl = normalizeImageUrl(parsed.value);
                const scope = canvasViewMode === 'detail' ? 'detail' : 'card';
                pushHistoryNow(scope === 'detail' ? 'Drop pop-up image asset' : 'Drop card image asset', form, layout);
                applyImageUrlForScope(normalizedUrl, scope, scope === 'card' ? parsed.iconKey : undefined);
                setAssetLibrary((current) =>
                    mergePersistentAssets(current, [{
                        id: `drop-${Date.now()}`,
                        label: parsed.label || (scope === 'detail' ? 'Dropped pop-up image' : 'Dropped card image'),
                        type: 'image-url',
                        preview: normalizedUrl,
                        value: normalizedUrl,
                        iconKey: scope === 'card' ? parsed.iconKey : undefined,
                        createdAt: Date.now(),
                    }]),
                );
                setFeedback(scope === 'detail' ? 'Image applied to the pop-up.' : 'Image applied to the card.');
                markDirty();
                return;
            }

            if (parsed?.type === 'icon-key') {
                pushHistoryNow('Drop logo asset', form, layout);
                setForm((current) => ({
                    ...current,
                    iconKey: parsed.value,
                }));
                setAssetLibrary((current) =>
                    mergePersistentAssets(current, [{
                        id: `icon-${parsed.value}`,
                        label: parsed.label || parsed.value,
                        type: 'icon-key',
                        preview: CONTENT_ICON_MAP[parsed.value],
                        value: parsed.value,
                        createdAt: Date.now(),
                    }]),
                );
                setFeedback('Logo asset dropped onto the canvas.');
                markDirty();
            }
        } catch {
            setFeedback('Could not read the dragged asset.');
        }
    }

    function handleMediaPanelDrop(event) {
        event.preventDefault();
        setMediaDropActive(false);
        if (event.dataTransfer?.files?.length) {
            handleUpload({ dataTransfer: event.dataTransfer }, canvasViewMode === 'detail' ? 'detail' : 'card');
        }
    }

    async function handleSave() {
        flushQueuedHistory();
        setIsSaving(true);
        setFeedback('Saving card...');
        try {
            const payload = {
                ...form,
                imageUrl: normalizeImageUrl(form.imageUrl),
                publishStartUtc: form.publishStartUtc || null,
                publishEndUtc: form.publishEndUtc || null,
                layoutJson: serializeLayout(layout),
            };
            const response = await saveContentCard(form.id, payload);
            if (!response?.success || !response?.data) {
                throw new Error(response?.errors?.[0] || 'Could not save the card.');
            }

            const saved = normalizeCard(response.data, response.data.section || CONTENT_SECTIONS.spotlight);
            setFeedback('Card saved.');
            await loadCards();
            setSelectedId(saved.id);
            setForm(saved);
            setLayout(parseLayoutJson(saved.layoutJson, saved.section));
            setIsDirty(false);
            setHistoryPast([]);
            setHistoryFuture([]);
        } catch (error) {
            setFeedback(error.message || 'Could not save the card.');
        } finally {
            setIsSaving(false);
        }
    }

    async function handleDelete(cardId = form.id) {
        if (!cardId) {
            handleCreateNew(form.section || CONTENT_SECTIONS.spotlight);
            return;
        }

        const confirmed = window.confirm('Delete this card permanently?');
        if (!confirmed) {
            return;
        }

        setFeedback('Deleting card...');
        try {
            const response = await deleteContentCard(cardId);
            if (!response?.success) {
                throw new Error(response?.errors?.[0] || 'Delete failed.');
            }
            setFeedback('Card deleted.');
            await loadCards();
        } catch (error) {
            setFeedback(error.message || 'Delete failed.');
        }
    }

    function handleDuplicate() {
        const sourceCards = cards.filter((item) => item.section === form.section);
        const nextOrder = sourceCards.length ? Math.max(...sourceCards.map((item) => item.displayOrder || 0)) + 1 : 1;
        const editableCopy = form.actionType === 'modal'
            ? createEditableDetailFromBuiltin(form.actionValue, form.title)
            : null;
        setSelectedId(null);
        setForm((current) => ({
            ...current,
            id: null,
            title: current.title ? `${current.title} (Copy)` : '',
            displayOrder: nextOrder,
            isPublished: false,
            actionType: editableCopy ? 'detail' : current.actionType,
            actionValue: editableCopy ? '' : current.actionValue,
            detailTitle: editableCopy?.detailTitle || current.detailTitle,
            detailContentHtml: editableCopy?.detailContentHtml || current.detailContentHtml,
        }));
        if (editableCopy) {
            setCanvasViewMode('detail');
        }
        setFeedback('Duplicated as a draft. Save to persist it.');
        setIsDirty(true);
        setHistoryPast([]);
        setHistoryFuture([]);
    }

    function handleConvertBuiltInModalToEditable() {
        if (form.actionType !== 'modal' || !form.actionValue) {
            return;
        }

        const editableCopy = createEditableDetailFromBuiltin(form.actionValue, form.title);
        pushHistoryNow('Convert built-in modal', form, layout);
        setForm((current) => ({
            ...current,
            actionType: 'detail',
            actionValue: '',
            detailTitle: current.detailTitle || editableCopy.detailTitle,
            detailContentHtml: current.detailContentHtml || editableCopy.detailContentHtml,
        }));
        setCanvasViewMode('detail');
        setSelectedLayer('title');
        setSelectedLayers(['title']);
        setFeedback('Built-in pop-up converted into an editable pop-up for this card.');
        markDirty();
    }

    function handleRestoreHistoryEntry(entry) {
        if (!entry) {
            return;
        }

        flushQueuedHistory();
        setHistoryFuture((current) => [cloneSnapshot(form, layout), ...current].slice(0, 100));
        setForm(entry.form);
        setLayout(entry.layout);
        setIsDirty(true);
        setFeedback(`Restored: ${entry.label}`);
    }

    const builtInDetail = useMemo(
        () => (form.actionType === 'modal' && form.actionValue
            ? createEditableDetailFromBuiltin(form.actionValue, form.title)
            : null),
        [form.actionType, form.actionValue, form.title],
    );
    const isBuiltInModalDetailView = canvasViewMode === 'detail' && form.actionType === 'modal' && Boolean(form.actionValue);
    const previewCard = {
        ...form,
        id: form.id || 'preview',
        detailTitle: builtInDetail?.detailTitle || form.detailTitle,
        detailContentHtml: builtInDetail?.detailContentHtml || form.detailContentHtml,
        detailImageUrl: layout.detail?.imageUrl,
    };
    const primaryCanvasTitle = canvasViewMode === 'detail' ? 'Pop-up canvas' : 'Card canvas';
    const primaryCanvasHelper = canvasViewMode === 'detail'
        ? 'Design the pop-up itself. Keep hierarchy and spacing clean.'
        : 'This is the main editing surface. The card should stay front and center.';
    const activeWorkspaceTool = WORKSPACE_TOOLS.find((tool) => tool.key === workspaceTool) || WORKSPACE_TOOLS[0];
    const showCardsPanel = workspaceTool === 'cards' && !sidebarCollapsed;
    const availablePanelSections = WORKSPACE_PANEL_SECTIONS[inspectorMode] || [];
    const selectedLayerLabel = studioLayers.find((item) => item.id === selectedLayer)?.label || (canvasViewMode === 'detail' ? 'Pop-up title' : 'Headline');
    const currentActionMeta = ACTION_TYPES.find((item) => item.value === form.actionType) || ACTION_TYPES[0];
    const currentBadgeVariant = BADGE_VARIANTS.find((item) => item.value === form.badgeVariant) || BADGE_VARIANTS[0];
    const assetOptions = ICON_OPTIONS.filter((option) => option.value && option.value !== 'uploaded-image');
    const recentHistoryEntries = historyPast.slice(-6).reverse();
    const recentMediaAssets = useMemo(() => {
        const currentAssets = form.imageUrl
            ? [{
                id: 'uploaded-main-image',
                label: 'Card image',
                type: 'image-url',
                preview: form.imageUrl,
                value: form.imageUrl,
                iconKey: form.iconKey === 'uploaded-image' ? 'uploaded-image' : undefined,
                createdAt: Date.now(),
              }]
            : [];
        const detailAssets = layout.detail?.imageUrl
            ? [{
                id: 'uploaded-detail-image',
                label: 'Pop-up image',
                type: 'image-url',
                preview: layout.detail.imageUrl,
                value: layout.detail.imageUrl,
                createdAt: Date.now(),
              }]
            : [];

        const builtAssets = assetOptions.slice(0, 8).map((option) => ({
            id: option.value,
            label: option.label,
            type: 'icon-key',
            preview: CONTENT_ICON_MAP[option.value],
            value: option.value,
            createdAt: 0,
        }));

        return mergePersistentAssets(assetLibrary, [...currentAssets, ...detailAssets, ...builtAssets]);
    }, [assetLibrary, assetOptions, form.iconKey, form.imageUrl, layout.detail?.imageUrl]);

    useEffect(() => {
        const fallback = DEFAULT_PANEL_SECTION[inspectorMode];
        if (!fallback) {
            return;
        }

        if (!availablePanelSections.some((item) => item.key === panelSection)) {
            setPanelSection(fallback);
        }
    }, [availablePanelSections, panelSection, inspectorMode]);

    return (
        <AnimatedPageShell ref={pageRef} className="admin-content-page-shell">
            <AdminPreviewDetailModal card={previewCard} open={previewOpen} onClose={() => setPreviewOpen(false)} />
            <div className="admin-content-page">
                {isMobileAdminBlocked ? (
                    <section className="admin-mobile-blocker glass-panel">
                        <span className="admin-mobile-blocker__eyebrow">Admin panel</span>
                        <h1>Please use a desktop PC</h1>
                        <p>
                            This workspace is only available on desktop. Open the Admin Panel from a laptop or desktop
                            computer to manage cards, layouts and publishing safely.
                        </p>
                    </section>
                ) : (
                    <>
                <header className="admin-design-topbar">
                    <div className="admin-design-topbar__title">
                        <p className="admin-content-kicker">{workspaceTool === 'admins' ? 'Admin Access' : 'Content Studio'}</p>
                        <h1>{workspaceTool === 'admins' ? 'Admin users' : selectedId ? form.title || 'Untitled card' : 'New card'}</h1>
                        <span>
                            {workspaceTool === 'admins'
                                ? 'Manage who receives the Admin role after Microsoft Entra ID authentication.'
                                : `${SECTION_META[form.section]?.label} - ${canvasViewMode === 'detail' ? 'Custom pop-up' : 'Main card'} - Editing ${selectedLayerLabel}`}
                        </span>
                    </div>
                    <div className="admin-design-topbar__actions">
                        {workspaceTool === 'admins' ? (
                            <button type="button" className="admin-secondary-btn" onClick={loadAdmins}>
                                <FiRefreshCw /> Refresh admins
                            </button>
                        ) : (
                            <>
                                <button type="button" className="admin-secondary-btn admin-design-topbar__accentBtn" onClick={() => handleCreateNew(form.section || CONTENT_SECTIONS.spotlight)}>
                                    <FiPlus /> New card
                                </button>
                                <button type="button" className="admin-secondary-btn" onClick={() => loadCards()}>
                                    <FiRefreshCw /> Refresh
                                </button>
                                <button type="button" className="admin-primary-btn" onClick={handleSave} disabled={isSaving}>
                                    <FiSave /> {isSaving ? 'Saving...' : isDirty ? 'Save changes' : 'Saved'}
                                </button>
                            </>
                        )}
                    </div>
                </header>

                <div className={`admin-studio-layout ${showCardsPanel ? '' : 'is-sidebar-collapsed'} ${inspectorCollapsed ? 'is-inspector-collapsed' : ''}`}>
                    <div ref={sidebarShellRef} className={`admin-content-sidebar-shell admin-content-sidebar-shell--design ${showCardsPanel ? '' : 'is-collapsed'}`}>
                        <aside className="admin-design-rail">
                            <button
                                ref={sidebarToggleRef}
                                type="button"
                                className={`admin-design-rail__toggle ${showCardsPanel ? '' : 'is-collapsed'}`}
                                onClick={() => {
                                    if (workspaceTool !== 'cards') {
                                        setWorkspaceTool('cards');
                                        setSidebarCollapsed(false);
                                        return;
                                    }

                                    setSidebarCollapsed((current) => !current);
                                }}
                                aria-label={showCardsPanel ? 'Hide cards panel' : 'Show cards panel'}
                                title={showCardsPanel ? 'Hide cards panel' : 'Show cards panel'}
                            >
                                {showCardsPanel ? <FiChevronLeft /> : <FiChevronRight />}
                            </button>
                            {WORKSPACE_TOOLS.map((tool) => {
                                const Icon = tool.icon;
                                const isActive = tool.key === 'cards'
                                    ? workspaceTool === 'cards'
                                    : workspaceTool !== 'cards' && inspectorMode === tool.key;

                                return (
                                    <button
                                        key={tool.key}
                                        type="button"
                                        className={`admin-design-rail__button ${isActive ? 'is-active' : ''}`}
                                        onClick={() => {
                                            if (tool.key === 'cards') {
                                                setWorkspaceTool('cards');
                                                setSidebarCollapsed(false);
                                                return;
                                            }

                                            setWorkspaceTool(tool.key);
                                            setInspectorMode(tool.key);
                                            setSidebarCollapsed(true);
                                        }}
                                        aria-pressed={isActive}
                                        title={tool.label}
                                    >
                                        <Icon />
                                        <span>{tool.label}</span>
                                    </button>
                                );
                            })}
                        </aside>
                        <aside ref={sidebarPanelRef} className="admin-content-sidebar admin-content-sidebar--design">
                            {showCardsPanel ? (
                                <>
                                    <div className="admin-content-sidebar__header admin-content-sidebar__header--design">
                                        <div>
                                            <h2>{activeWorkspaceTool.title}</h2>
                                            <span>{activeWorkspaceTool.description}</span>
                                        </div>
                                        <div className="admin-content-sidebar__totals">
                                            <strong>{cards.length}</strong>
                                            <span>items</span>
                                        </div>
                                    </div>
                                    <div className="admin-content-sidebar__scroll">
                                        <div className="admin-content-sidebar__search">
                                            <FiSearch />
                                            <input
                                                type="search"
                                                value={sidebarQuery}
                                                onChange={(event) => setSidebarQuery(event.target.value)}
                                                placeholder="Search cards by title, badge or CTA"
                                                aria-label="Search cards"
                                            />
                                        </div>
                                        <div className="admin-content-list">
                                            {Object.entries(SECTION_META).map(([section, meta]) => (
                                                <div className="admin-content-group" key={section}>
                                                    <div className="admin-content-group__header">
                                                        <button
                                                            type="button"
                                                            className={`admin-content-group__trigger ${sectionCollapsed[section] ? 'is-collapsed' : ''}`}
                                                            onClick={() =>
                                                                setSectionCollapsed((current) => ({
                                                                    ...current,
                                                                    [section]: !current[section],
                                                                }))
                                                            }
                                                            aria-expanded={!sectionCollapsed[section]}
                                                        >
                                                            <FiChevronDown />
                                                            <div className="admin-content-group__summary">
                                                                <strong>{meta.label}</strong>
                                                            </div>
                                                            <span className={`admin-content-group__count admin-content-group__count--${section}`}>
                                                                {cardsBySection[section].length} cards
                                                            </span>
                                                        </button>
                                                        <button type="button" className="admin-list-action" onClick={() => handleCreateNew(section)}>
                                                            <FiPlus /> Add
                                                        </button>
                                                    </div>
                                                    <div
                                                        ref={(node) => {
                                                            sidebarSectionRefs.current[section] = node;
                                                        }}
                                                        className="admin-content-group__body"
                                                    >
                                                        {filteredCardsBySection[section].map((card) => {
                                                            const isActive = selectedId === card.id;
                                                            const badgeLabel = card.badgeText || 'No badge';
                                                            const publishLabel = card.isPublished ? 'Published' : 'Draft';

                                                            return (
                                                                <button
                                                                    key={card.id}
                                                                    type="button"
                                                                    className={`admin-content-list__item ${isActive ? 'is-active' : ''}`}
                                                                    onClick={() => handleSelectCard(card)}
                                                                >
                                                                    <div className="admin-content-list__itemTop">
                                                                        <div className="admin-content-list__itemMeta">
                                                                            <small>{meta.label}</small>
                                                                            <span className="admin-content-list__itemOrder">Order {card.displayOrder || 1}</span>
                                                                        </div>
                                                                        <span className={`admin-content-list__itemStatus ${isActive ? 'is-active' : ''}`}>
                                                                            {isActive ? 'Editing now' : publishLabel}
                                                                        </span>
                                                                    </div>
                                                                    <strong>{card.title || 'Untitled card'}</strong>
                                                                    <div className="admin-content-list__chips">
                                                                        <span className="admin-content-list__chip">{badgeLabel}</span>
                                                                        {!isActive ? <span className="admin-content-list__chip admin-content-list__chip--ghost">{publishLabel}</span> : null}
                                                                    </div>
                                                                    <div className="admin-content-list__actions">
                                                                        <span className="admin-list-action">Edit</span>
                                                                        <span
                                                                            className="admin-list-action is-danger"
                                                                            onClick={(event) => {
                                                                                event.stopPropagation();
                                                                                handleDelete(card.id);
                                                                            }}
                                                                        >
                                                                            Delete
                                                                        </span>
                                                                    </div>
                                                                </button>
                                                            );
                                                        })}
                                                        {!filteredCardsBySection[section].length ? (
                                                            <div className="admin-empty-state admin-empty-state--section">
                                                                {normalizedSidebarQuery ? 'No cards match this search.' : 'This section has no cards yet.'}
                                                            </div>
                                                        ) : null}
                                                    </div>
                                                </div>
                                            ))}
                                            {!isLoading && cards.length === 0 ? <div className="admin-empty-state">No cards created yet.</div> : null}
                                        </div>
                                    </div>
                                </>
                            ) : null}
                        </aside>
                    </div>

                    <section className="admin-studio-main">
                        {workspaceTool === 'admins' ? (
                            <AdminAccessPanel
                                admins={admins}
                                adminEmail={adminEmail}
                                adminFeedback={adminFeedback}
                                adminLoading={adminLoading}
                                adminSaving={adminSaving}
                                onEmailChange={setAdminEmail}
                                onCreate={handleCreateAdmin}
                                onRefresh={loadAdmins}
                                onActivate={handleActivateAdmin}
                                onDeactivate={handleDeactivateAdmin}
                                onRemove={handleRemoveAdmin}
                            />
                        ) : (
                            <>
                        <div className="admin-studio-toolbar">
                            <div>
                                <h2>{canvasViewMode === 'detail' ? 'Custom pop-up canvas' : 'Card canvas'}</h2>
                                <p>{SECTION_META[form.section]?.description} The canvas is the main workspace. Use the rail to switch context and the right panel for precise editing.</p>
                            </div>
                            <div className="admin-editor-toolbar__actions">
                                <button
                                    type="button"
                                    className={`admin-secondary-btn admin-editor-toggle ${inspectorCollapsed ? 'is-collapsed' : ''}`}
                                    onClick={() => setInspectorCollapsed((current) => !current)}
                                >
                                    <span className="admin-editor-toggle__icon">
                                        {inspectorCollapsed ? <FiChevronLeft /> : <FiChevronRight />}
                                    </span>
                                    <span className="admin-editor-toggle__copy">
                                        <strong>{inspectorCollapsed ? 'Show tools' : 'Focus mode'}</strong>
                                        <small>{inspectorCollapsed ? 'Bring the editor back.' : 'Hide the right panel and expand the canvas.'}</small>
                                    </span>
                                </button>
                                <button type="button" className="admin-secondary-btn" onClick={handleDuplicate}>
                                    <FiCopy /> Duplicate
                                </button>
                                <button type="button" className="admin-secondary-btn admin-danger-btn" onClick={() => handleDelete(form.id)}>
                                    <FiTrash2 /> Delete
                                </button>
                            </div>
                        </div>

                        {feedback ? <div className="admin-feedback-banner">{feedback}</div> : null}

                                                <div className="admin-studio-workspace">
                            <div className="admin-studio-canvasPanel">
                                <div className="admin-studio-canvasHeader">
                                    <div>
                                        <strong>{primaryCanvasTitle}</strong>
                                        <span>{primaryCanvasHelper}</span>
                                    </div>
                                    <div className="admin-studio-canvasControls">
                                        <button
                                            type="button"
                                            className={`admin-studio-templateChip ${canvasViewMode === 'card' ? 'is-active' : ''}`}
                                            onClick={() => setCanvasViewMode('card')}
                                        >
                                            Card view
                                        </button>
                                        <button
                                            type="button"
                                            className={`admin-studio-templateChip ${canvasViewMode === 'detail' ? 'is-active' : ''}`}
                                            onClick={() => setCanvasViewMode('detail')}
                                            disabled={!['detail', 'modal'].includes(form.actionType)}
                                        >
                                            Pop-up view
                                        </button>
                                    </div>
                                    <div className="admin-studio-canvasDocActions">
                                        <button type="button" className="admin-secondary-btn" onClick={handleUndo} disabled={!historyPast.length}>
                                            <FiCornerDownLeft /> Undo
                                        </button>
                                        <button type="button" className="admin-secondary-btn" onClick={handleRedo} disabled={!historyFuture.length}>
                                            <FiCornerDownRight /> Redo
                                        </button>
                                        <button type="button" className="admin-secondary-btn" onClick={() => setPreviewOpen(true)}>
                                            <FiEye /> Preview
                                        </button>
                                    </div>
                                    <div className="admin-studio-focusPill">
                                        <FiLayers /> {selectionIds.length > 1
                                            ? `${selectionIds.length} layers selected`
                                            : `Editing: ${studioLayers.find((item) => item.id === selectedLayer)?.label || (canvasViewMode === 'detail' ? 'Pop-up title' : 'Headline')}`}
                                    </div>
                                </div>
                                <div className="admin-studio-canvasDeck">
                                    <div className="admin-studio-canvasMain">
                                        {isBuiltInModalDetailView ? (
                                            <div className="admin-studio-builtinWorkspace">
                                                <div className="admin-studio-builtinWorkspace__note">
                                                    <div>
                                                        <strong>Prebuilt pop-up preview</strong>
                                                        <span>This modal uses the real embedded component, including its imagery and layout. Convert it if you want full canvas editing.</span>
                                                    </div>
                                                    <button type="button" className="admin-secondary-btn" onClick={handleConvertBuiltInModalToEditable}>
                                                        Convert to editable pop-up
                                                    </button>
                                                </div>
                                                <BuiltInModalScaledPreview modalKey={form.actionValue} />
                                            </div>
                                        ) : (
                                            <CardStudioCanvas
                                                card={previewCard}
                                                layout={layout}
                                                viewMode={canvasViewMode}
                                                selectedLayer={selectedLayer}
                                                selectedLayers={selectedLayers}
                                                onSelectLayer={handleLayerSelection}
                                                onLayoutElementChange={updateLayoutElement}
                                                onDuplicateLayer={handleDuplicateLayer}
                                                onDeleteLayer={handleDeleteLayer}
                                                onToggleLayerVisibility={handleToggleLayerVisibility}
                                                onAssetDrop={handleCanvasAssetDrop}
                                                onInlineTextChange={handleInlineTextChange}
                                                onAlignSelection={handleAlignSelection}
                                                onArrangeSelection={handleArrangeSelection}
                                                onApplyImageFramePreset={handleApplyImageFramePreset}
                                                onNudgeImageCrop={handleNudgeImageCrop}
                                            />
                                        )}
                                    </div>
                                </div>
                            </div>

                            <CardStudioInspector
                                form={previewCard}
                                layout={layout}
                                viewMode={canvasViewMode}
                                onChangeViewMode={setCanvasViewMode}
                                editorMode={inspectorMode}
                                onEditorModeChange={setInspectorMode}
                                showModeTabs
                                selectedLayer={selectedLayer}
                                selectedLayers={selectionIds}
                                studioLayers={studioLayers}
                                onSelectLayer={handleLayerSelection}
                                onUpdateField={updateField}
                                onUpdateLayout={updateLayoutElement}
                                onUpload={handleUpload}
                                onUpdateDetailImageUrl={updateDetailImageUrl}
                                isUploading={isUploading}
                                onResetTemplate={handleResetTemplate}
                                onDuplicateLayer={handleDuplicateLayer}
                                onDeleteLayer={handleDeleteLayer}
                                onToggleLayerVisibility={handleToggleLayerVisibility}
                                onArrangeLayer={(layerId, mode) => handleArrangeLayers([layerId], mode)}
                                onReorderLayer={handleReorderLayer}
                                onConvertBuiltInModal={handleConvertBuiltInModalToEditable}
                                onUpdateCardSurface={updateCardSurface}
                                onAlignSelection={handleAlignSelection}
                                onDistributeSelection={handleDistributeSelection}
                                onArrangeSelection={handleArrangeSelection}
                                onApplyTypographyPreset={handleApplyTypographyPreset}
                                onApplyPalettePreset={handleApplyPalettePreset}
                                onAddTextLayer={handleAddTextLayer}
                                onApplyImageFramePreset={handleApplyImageFramePreset}
                                onNudgeImageCrop={handleNudgeImageCrop}
                                draggedLayerId={draggedLayerId}
                                layerDropTarget={layerDropTarget}
                                historyEntries={historyPast.slice(-8).reverse()}
                                onRestoreHistoryEntry={handleRestoreHistoryEntry}
                            />
                        </div>
                            </>
                        )}
                    </section>
                </div>
                    </>
                )}
            </div>
        </AnimatedPageShell>
    );
}
