import './Modal.css';

const Modal = ({ isOpen, onClose, children }) => {
    if (!isOpen) {
        return null;
    }
    return (
        <div className="modal-overlay" onClick={onClose}>
            <div
                className="modal-content-Logger"
                onClick={(e) => e.stopPropagation()}
            >
                <button
                    className="modal-close-button"
                    onClick={onClose}
                ></button>
                <div className="modal-body">{children}</div>
            </div>
        </div>
    );
};

export default Modal;
