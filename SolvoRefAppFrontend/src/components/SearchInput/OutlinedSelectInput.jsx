import './OutlinedSelectInput.css';
import {
    useEffect,
    useLayoutEffect,
    useMemo,
    useRef,
    useState,
    forwardRef,
    useImperativeHandle,
} from 'react';
import { createPortal } from 'react-dom';
import { toast } from 'react-toastify';
import { Tooltip } from 'react-tooltip';

const OutlinedSelectInput = forwardRef(
    (
        {
            id,
            label,
            placeholder = '',
            required = false,
            className = '',
            inputClassName = '',

            // normal input
            isSearch = false,
            type = 'text',
            value = '',
            onChange,

            // search mode
            options = [],
            onOptionSelect,
            noResultsText = 'No results found',
            searchPlaceholder = 'Search...',
            filterFn,

            // valor por defecto confirmado
            defaultOption = null, // { value, text } | null

            // clases de variante
            variantClassName = '',

            // opcional: mensaje de required
            requiredMessage,

            // ===============================
            // 🔹 API de validación externa
            // ===============================
            externalValidate, // (ctx) => true | string | { valid: boolean, error?: string }
            onInvalid, // (message) => void
            showToastOnError = true,

            // ===============================
            // 🔹 NUEVO: comportamiento de validación
            // ===============================
            defaultInvalid = true, // ⬅ inicia con error=true
            validateOnChange = true, // ⬅ valida en onChange
            debounceDelay = 300, // ⬅ debounce para validar en input/search
            onValidateChange, // ⬅ (isValid: boolean) => void  - avisa al padre
            toolTipText = '',
        },
        ref,
    ) => {
        const rootRef = useRef(null);
        const inputRef = useRef(null);
        const menuRef = useRef(null);

        const [searchTerm, setSearchTerm] = useState('');
        const [showDropdown, setShowDropdown] = useState(false);
        const [activeIndex, setActiveIndex] = useState(-1);
        const [menuStyle, setMenuStyle] = useState(null);

        const [isError, setIsError] = useState(!!defaultInvalid);
        const [errorMessage, setErrorMessage] = useState('');

        // timers para debounce (separados por claridad)
        const debounceTimerRef = useRef(null);

        // Opción confirmada actual
        const [currentOption, setCurrentOption] = useState(defaultOption);

        // Notificar al montar el estado inicial (invalid si defaultInvalid=true)
        useEffect(() => {
            if (onValidateChange) onValidateChange(!defaultInvalid);
            if (defaultInvalid) {
                validate();
                setErrorMessage('');
            } // true=valido/false=invalido
            // eslint-disable-next-line react-hooks/exhaustive-deps
        }, []);

        // Sincroniza cambios externos del defaultOption
        useEffect(() => {
            setCurrentOption(defaultOption || null);
            if (isSearch) setSearchTerm(defaultOption?.text ?? '');
        }, [defaultOption, isSearch]);

        // RESET cuando cambian las options

        useEffect(() => {
            if (!isSearch) return;

            // Solo resetear si la opción actual ya no existe en el nuevo listado
            const stillExists = currentOption
                ? options.some((o) => o.value === currentOption.value)
                : false;

            if (!stillExists) {
                setCurrentOption(null);
                setSearchTerm('');
                setShowDropdown(false);
                setActiveIndex(-1);
                if (defaultInvalid) {
                    validate();
                    setErrorMessage('');
                }
            }
        }, [options, isSearch, currentOption]);

        // Limpia timer al desmontar
        useEffect(() => {
            return () => {
                if (debounceTimerRef.current)
                    clearTimeout(debounceTimerRef.current);
            };
        }, []);

        // Filtrado por defecto: por .text
        const defaultFilter = (opt, term) =>
            opt.text.toLowerCase().includes(term.trim().toLowerCase());

        const filteredOptions = useMemo(() => {
            if (!isSearch) return [];
            if (!searchTerm) return options;
            const f = filterFn || defaultFilter;
            return options.filter((o) => f(o, searchTerm));
        }, [isSearch, options, searchTerm, filterFn]);

        // Click fuera: cierra y restaura el valor confirmado
        useEffect(() => {
            if (!isSearch) return;
            const handler = (e) => {
                const clickedInsideRoot =
                    rootRef.current && rootRef.current.contains(e.target);
                const clickedInsideMenu =
                    menuRef.current && menuRef.current.contains(e.target);

                if (!clickedInsideRoot && !clickedInsideMenu) {
                    setShowDropdown(false);
                    setActiveIndex(-1);
                    setSearchTerm(currentOption?.text ?? '');
                }
            };
            document.addEventListener('mousedown', handler);
            return () => document.removeEventListener('mousedown', handler);
        }, [isSearch, currentOption]);

        useLayoutEffect(() => {
            if (!isSearch || !showDropdown || !inputRef.current) return;

            const updateMenuPosition = () => {
                if (!inputRef.current) return;

                const rect = inputRef.current.getBoundingClientRect();
                const gap = 6;
                const viewportPadding = 12;
                const availableBelow =
                    window.innerHeight - rect.bottom - gap - viewportPadding;
                const maxHeight = Math.max(
                    120,
                    Math.min(240, Math.max(availableBelow, 120)),
                );

                setMenuStyle({
                    position: 'fixed',
                    left: `${rect.left}px`,
                    width: `${rect.width}px`,
                    top: `${rect.bottom + gap}px`,
                    maxHeight: `${maxHeight}px`,
                    zIndex: 2000,
                });
            };

            updateMenuPosition();
            window.addEventListener('resize', updateMenuPosition);
            window.addEventListener('scroll', updateMenuPosition, true);

            return () => {
                window.removeEventListener('resize', updateMenuPosition);
                window.removeEventListener('scroll', updateMenuPosition, true);
            };
        }, [isSearch, showDropdown, filteredOptions.length]);

        // --- Validación ---

        const getIsEmpty = (override) => {
            if (!required) return false;

            if (isSearch) {
                const opt = override?.currentOption ?? currentOption;
                return !opt || !opt.text?.trim();
            }

            const val = override?.value ?? value;
            return !String(val ?? '').trim();
        };

        // showError con posibilidad de silenciar toast (para evitar spam en onChange)
        const showError = (msg, { silent = false } = {}) => {
            setIsError(true);
            setErrorMessage(msg || `${label || 'Field'} invalid`);
            if (!silent && showToastOnError && msg) toast.error(msg);
            if (onInvalid) onInvalid(msg);
        };

        const fireRequiredError = (opts) => {
            const msg = requiredMessage || `${label || 'Field'} required`;
            showError(msg, opts);
        };

        // Ejecuta validación externa si está definida

        const runExternalValidation = (opts, override) => {
            if (!externalValidate) return true;

            const ctx = {
                isSearch,
                value: override?.value ?? value, // <-- usa override si viene
                currentOption: override?.currentOption ?? currentOption,
                searchTerm: override?.searchTerm ?? searchTerm,
                options,
                label,
                id,
            };

            try {
                const res = externalValidate(ctx);

                if (res === true) return true;

                if (typeof res === 'string') {
                    showError(res, opts);
                    return false;
                }
                if (res && typeof res === 'object') {
                    if (res.valid) return true;
                    showError(res.error || `${label || 'Field'} invalid`, opts);
                    return false;
                }
                return true; // cualquier otro retorno lo tratamos como válido
            } catch {
                showError(`${label || 'Field'} invalid`, opts);
                return false;
            }
        };

        // Valida y notifica al padre (con opción de silencio de toast)

        const validateInternal = ({ silent = false, override } = {}) => {
            // 1) Requerido
            if (required && getIsEmpty(override)) {
                fireRequiredError({ silent });
                onValidateChange && onValidateChange(false);
                return false;
            }
            // 2) Validación externa
            const okExternal = runExternalValidation({ silent }, override);
            if (!okExternal) {
                onValidateChange && onValidateChange(false);
                return false;
            }

            // 3) Ok
            setIsError(false);
            setErrorMessage('');
            onValidateChange && onValidateChange(true);
            return true;
        };

        // API pública: validate() con toasts (no silencioso)
        const validate = () => validateInternal({ silent: false });

        // Exponer métodos al padre
        useImperativeHandle(ref, () => ({
            validate,
            clearError: () => {
                setIsError(false);
                setErrorMessage('');
                onValidateChange && onValidateChange(true);
            },
            setError: (msg) => {
                showError(msg || `${label || 'Field'} invalid`);
                onValidateChange && onValidateChange(false);
            },
            setExternalError: (msg) => {
                showError(msg || `${label || 'Field'} invalid`);
                onValidateChange && onValidateChange(false);
            },
        }));

        // Navegación por teclado
        const handleKeyDown = (e) => {
            if (!isSearch || !showDropdown) return;

            if (e.key === 'ArrowDown') {
                e.preventDefault();
                setActiveIndex((prev) =>
                    prev < filteredOptions.length - 1 ? prev + 1 : 0,
                );
            } else if (e.key === 'ArrowUp') {
                e.preventDefault();
                setActiveIndex((prev) =>
                    prev > 0 ? prev - 1 : filteredOptions.length - 1,
                );
            } else if (e.key === 'Enter') {
                e.preventDefault();
                if (activeIndex >= 0 && activeIndex < filteredOptions.length) {
                    selectOption(filteredOptions[activeIndex]);
                }
            } else if (e.key === 'Escape') {
                e.preventDefault();
                setShowDropdown(false);
                setActiveIndex(-1);
                setSearchTerm(currentOption?.text ?? '');
                inputRef.current?.blur();
            }
        };

        const selectOption = (opt) => {
            setCurrentOption(opt);
            setSearchTerm(opt.text);
            setShowDropdown(false);
            setActiveIndex(-1);

            validateInternal({
                silent: false,
                override: { currentOption: opt, searchTerm: opt.text },
            });

            onOptionSelect && onOptionSelect(opt);

            setTimeout(() => {
                inputRef.current?.blur();
            }, 0);
        };

        // Desactivar autocomplete de forma robusta
        const noAutoCompleteProps = {
            autoComplete: 'off',
            autoCapitalize: 'off',
            autoCorrect: 'off',
            spellCheck: false,
            name: `${id || 'input'}-${Math.random().toString(36).slice(2, 8)}`,
            inputMode: 'text',
        };

        const containerClasses = [
            'outlined-input-container',
            variantClassName,
            isSearch && showDropdown ? 'is-open' : '',
            isError ? 'has-error' : '',
            className,
        ]
            .filter(Boolean)
            .join(' ');

        const inputClasses = [
            isSearch ? 'custom-country-code-input' : '',
            'input-form',
            isError ? 'input-error' : '',
            inputClassName,
        ]
            .filter(Boolean)
            .join(' ');

        // onChange con debounce para validar (aplica a input y a search)
        const scheduleValidation = (silent = true, override) => {
            if (!validateOnChange) return;
            if (debounceTimerRef.current)
                clearTimeout(debounceTimerRef.current);
            debounceTimerRef.current = setTimeout(() => {
                validateInternal({ silent, override });
            }, debounceDelay);
        };

        return (
            <div
                className={containerClasses}
                ref={rootRef}
                role={isSearch ? 'combobox' : undefined}
                aria-expanded={isSearch ? showDropdown : undefined}
                aria-haspopup={isSearch ? 'listbox' : undefined}
                aria-invalid={isError || undefined}
                aria-errormessage={isError ? `${id}-err` : undefined}
            >
                {toolTipText.length > 0 && (
                    <Tooltip
                        id={id}
                        className="toolTipInput"
                        classNameArrow="toolTipInputArrow"
                        opacity={1}
                    />
                )}
                {!isSearch ? (
                    <>
                        {isError && errorMessage && (
                            <div id={`${id}-err`} className="input-error-text">
                                {errorMessage}
                            </div>
                        )}
                        <input
                            data-tooltip-id={id}
                            data-tooltip-content={toolTipText}
                            id={id}
                            type={type}
                            placeholder={placeholder}
                            value={value}
                            onChange={(e) => {
                                onChange && onChange(e); // controlado por el padre
                                scheduleValidation(true, {
                                    value: e.target.value,
                                });
                            }}
                            required={required}
                            className={inputClasses}
                            {...noAutoCompleteProps}
                        />
                        <fieldset className="input-outline">
                            <legend className="input-legend">
                                <span>
                                    {label}
                                    {required ? '*' : ''}
                                </span>
                            </legend>
                        </fieldset>
                    </>
                ) : (
                    <>
                        {isError && errorMessage && (
                            <div id={`${id}-err`} className="input-error-text">
                                {errorMessage}
                            </div>
                        )}
                        <input
                            data-tooltip-id={id}
                            data-tooltip-content={toolTipText}
                            id={id}
                            ref={inputRef}
                            type="text"
                            placeholder={placeholder || searchPlaceholder}
                            value={searchTerm}
                            onChange={(e) => {
                                setSearchTerm(e.target.value);
                                if (!showDropdown) setShowDropdown(true);
                                setActiveIndex(-1);
                                scheduleValidation(true, {
                                    searchTerm: e.target.value,
                                    currentOption: null,
                                });
                            }}
                            onClick={() => {
                                setSearchTerm('');
                                setActiveIndex(-1);
                                setShowDropdown(true);
                            }}
                            onFocus={() => {
                                setSearchTerm('');
                                setActiveIndex(-1);
                                setShowDropdown(true);
                            }}
                            onKeyDown={handleKeyDown}
                            required={required}
                            className={inputClasses}
                            aria-controls={
                                showDropdown ? `${id}-listbox` : undefined
                            }
                            aria-autocomplete="list"
                            {...noAutoCompleteProps}
                        />
                        <fieldset className="input-outline">
                            <legend className="input-legend">
                                <span>
                                    {label}
                                    {required ? '*' : ''}
                                </span>
                            </legend>
                        </fieldset>

                        {showDropdown &&
                            menuStyle &&
                            createPortal(
                                <ul
                                    id={`${id}-listbox`}
                                    ref={menuRef}
                                    className="custom-dropdown-menu custom-dropdown-menu--portal"
                                    role="listbox"
                                    style={menuStyle}
                                >
                                    {filteredOptions.length > 0 ? (
                                        filteredOptions.map((opt, idx) => (
                                            <li
                                                key={`${opt.value}-${idx}`}
                                                role="option"
                                                aria-selected={idx === activeIndex}
                                                className={`custom-dropdown-item ${
                                                    idx === activeIndex
                                                        ? 'active'
                                                        : ''
                                                }`}
                                                onMouseDown={(e) =>
                                                    e.preventDefault()
                                                }
                                                onClick={() => selectOption(opt)}
                                                onMouseEnter={() =>
                                                    setActiveIndex(idx)
                                                }
                                            >
                                                {opt.text}
                                            </li>
                                        ))
                                    ) : (
                                        <li className="custom-dropdown-no-results">
                                            {noResultsText}
                                        </li>
                                    )}
                                </ul>,
                                document.body,
                            )}
                    </>
                )}
            </div>
        );
    },
);

export default OutlinedSelectInput;
