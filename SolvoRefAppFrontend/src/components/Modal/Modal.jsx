import { useRef } from 'react';
import { Modal as RBModal } from 'react-bootstrap';
import { useModalMotion } from '../../animations/useModalMotion';
import closeIcon from '../../assets/icons/modal-close.svg';
import './Modal.css';

const Modal = ({
  showModal,
  children,
  handleCloseClick,
  size = "lg",
  centered = true,
  backdrop = true,
  keyboard = true,
}) => {
  const modalRef = useRef(null);
  useModalMotion(modalRef, showModal);

  return (
    <RBModal
      show={showModal}
      size={size}
      onHide={handleCloseClick}
      centered={centered}
      backdrop={backdrop}
      keyboard={keyboard}
    >
      <RBModal.Body ref={modalRef} data-modal-overlay>
        <div data-modal-panel style={{ position: 'relative' }}>
        <button
          type="button"
          className="app-modal-close position-absolute top-0 end-0 m-lg-3 cursor-pointer gsap-button"
          aria-label="Close"
          onClick={handleCloseClick}
        >
          <img src={closeIcon} alt="" aria-hidden="true" />
        </button>
        {children}
        </div>
      </RBModal.Body>
    </RBModal>
  );
};

export default Modal;
