import DOMPurify from 'dompurify';
import Modal from '../Modal/Modal';
import { resolveContentAssetUrl } from '../../config/api';
import { parseLayoutJson } from '../CardStudio/cardStudioTemplates';
import './ManagedContentModal.css';

function ManagedContentModal({ card, open, onClose }) {
    if (!card) {
        return null;
    }

    const title = card.detailTitle || card.title;
    const content = card.detailContentHtml || card.descriptionHtml || '';
    const parsedLayout = parseLayoutJson(card.layoutJson, card.section);
    const imageUrl = resolveContentAssetUrl(parsedLayout?.detail?.imageUrl || card.imageUrl);
    const summary = card.descriptionHtml || '<p>No extra detail has been added yet.</p>';

    return (
        <Modal showModal={open} handleCloseClick={onClose} size="lg" centered backdrop="static">
            <div className="managed-content-modal">
                <div className="managed-content-modal__hero">
                    {card.badgeText ? <span className={`managed-content-modal__badge badge-${card.badgeVariant || 'default'}`}>{card.badgeText}</span> : null}
                    <div className="managed-content-modal__heroCopy">
                        <h2>{title || 'Card preview'}</h2>
                        <div
                            className="managed-content-modal__summary"
                            dangerouslySetInnerHTML={{ __html: DOMPurify.sanitize(summary) }}
                        />
                    </div>
                </div>
                {imageUrl ? (
                    <img
                        src={imageUrl}
                        alt={title}
                        className="managed-content-modal__image"
                    />
                ) : null}
                <div
                    className="managed-content-modal__body"
                    dangerouslySetInnerHTML={{ __html: DOMPurify.sanitize(content || summary) }}
                />
            </div>
        </Modal>
    );
}

export default ManagedContentModal;
