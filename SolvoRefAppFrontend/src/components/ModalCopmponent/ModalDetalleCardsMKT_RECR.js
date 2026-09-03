import "./Modal_mkt_recr.css";
import equix from "../../assets/images/icons8-x-48.png";
import lupa from "../../assets/images/icons8-búsqueda-50.png";
import { getInitials } from "../CostantsComponent/getInitials";
import { formatTimeElapsed } from "../CostantsComponent/timeElapsed";
import { COLORS_ARRAY } from "../CostantsComponent/costants";
import coment from "../../assets/images/comentario.png";
import { useState } from "react";
import { useAuth } from "../AuthContextComponent/AuthContext";
import "bootstrap/dist/css/bootstrap.min.css";
import { TaAssignePeople } from "../CostantsComponent/TaAssignePeople";
import { lastUpdate } from "../CostantsComponent/TaAssignePeople";

const getUserColor = (user, usersArray, colorArray) => {
  if (!user || !usersArray || !colorArray || usersArray.length === 0) {
    return "#ccc";
  }
  const userIndex = usersArray.findIndex((u) => u.id === user.id);
  const colorIndex = userIndex !== -1 ? userIndex % colorArray.length : 0;
  return colorArray[colorIndex];
};

const formatDateTime = (timestamp) => {
  if (!timestamp) return "";
  const date = new Date(timestamp);
  const options = {
    year: "numeric",
    month: "short",
    day: "numeric",
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
    hour12: true,
  };
  return date.toLocaleString("en-US", options);
};

function Modal_MKT_RECR({
  card,
  onClose,
  mktUsers = [],
  recruiterUsers = [],
  view,
}) {
  const [opcionSelecionada, setOpcionSelecionada] = useState("");

  const handleCambio = (event) => {
    // Corrección: event.target.value en lugar de event.target.vaue
    setOpcionSelecionada(event.target.value);
  };

  const { userData } = useAuth();
  const [commentText, setCommentext] = useState("");
  const [comments, setComments] = useState([]);

  const handleAddComment = () => {
    if (commentText.trim() === "" || !userData) {
      return;
    }
    const usersToSearch = view === "recr" ? recruiterUsers : mktUsers;
    const userColor = getUserColor(userData, usersToSearch, COLORS_ARRAY);
    const newComment = {
      id: Date.now(),
      text: commentText,
      userInitials: getInitials(userData.name),
      userName: userData.name,
      userColor: userColor,
      timestamp: new Date(),
    };
    setComments((prevComments) => [
      newComment,
      ...(Array.isArray(prevComments) ? prevComments : []),
    ]);
    setCommentext("");
  };

  if (!card) {
    return null;
  }

  const mktAssigneeUser = mktUsers.find(
    (user) =>
      user.id === card.selectedMktUser?.id || user.id === card.assignedMktUserId
  );
  const taAssigneeUser = recruiterUsers.find(
    (user) => user.id === card.assignedRecruiterId
  );

  const renderUserBadge = (user) => {
    if (!user) return "No Asignado";
    const usersToSearch = view === "recr" ? recruiterUsers : mktUsers;
    const userIndex = usersToSearch.findIndex((u) => u.id === user.id);
    const userColor = COLORS_ARRAY[userIndex % COLORS_ARRAY.length];

    return (
      <>
        <span
          className="initial-user"
          style={{
            backgroundColor: userColor,
            marginRight: "4px",
          }}
        >
          {getInitials(user.name)}
        </span>
        {user.name}
      </>
    );
  };

  return (
    <>
      <div className="modal-overlay-mkt-recr" onClick={onClose}>
        <div
          className="modal-content-mkt-recr"
          onClick={(e) => e.stopPropagation()}
        >
          <div className="header-title-btn">
            <div className="title-mkt-recr">
              <h2 className="modal-title-mkt-recr">Referral info</h2>
            </div>
            <div className="close-btn-detail-referral">
              <button className="modal-close-button-mkt-recr" onClick={onClose}>
                <img alt="" src={equix} className="equix"></img>
              </button>
            </div>
          </div>
          <div className="paneles">
            <div className="panel-izquierdo">
              <div className="modal-body-mkt-recr">
                <span className="modal-label">Name</span>
                <span className="modal-value">{card.name}</span>
                <span className="modal-label">Email</span>
                <span className="modal-value">{card.email}</span>
                <span className="modal-label">Phone</span>
                <span className="modal-value">{card.phoneNumber}</span>
                {card.referredBy && (
                  <>
                    <span className="modal-label">Referral ID</span>
                    <span className="modal-value">{card.referralID}</span>
                  </>
                )}
                <span className="modal-label">Area</span>
                <span className="modal-value">{card.area}</span>
                <span className="modal-label">Country</span>
                <span className="modal-value">{card.country}</span>
                <span className="modal-label">City</span>
                <span className="modal-value">{card.city}</span>
                <span className="modal-label">Referred by</span>
                <span className="modal-value">{card.referredBy}</span>
              </div>
            </div>
            <div className="panel-derecho">
              <div className="titulo-panel-derecho">
                <img className="lupa2" src={lupa} alt=""></img>
                <h2 className="titulo">Details</h2>
              </div>
              <div className="aligh-derecho">
                <div className="modal-body-mkt-recr">
                  {view === "mkt" && (
                    <>
                      <span className="modal-label">Assignee</span>
                      <span className="modal-value">
                        {renderUserBadge(mktAssigneeUser)}
                      </span>
                    </>
                  )}
                  {view === "recr" && (
                    <>
                      <span className="modal-label">Assignee</span>
                      <span className="modal-value">
                        {renderUserBadge(taAssigneeUser)}
                      </span>
                    </>
                  )}
                  <span className="modal-label">Status</span>
                  <span className="modal-value">{card.status}</span>
                  <span className="modal-label">Creation date</span>
                  <span className="modal-value">{card.date}</span>
                  <span className="modal-label">Vigency</span>
                  <span className="modal-value">
                    {formatTimeElapsed(card.creationDate)} days
                  </span>

                  {/* CLAVE: CORRECCIÓN DE LA LÓGICA CONDICIONAL */}
                  {view === "recr" && card.status === "In Progress" && (
                    <>
                      <span className="modal-label">TA Assigne</span>
                      <div>
                        <select
                          id="selector"
                          value={opcionSelecionada}
                          onChange={handleCambio}
                          className="selector-taassigne"
                        >
                          <option value="" disabled>
                            -- Select an option --
                          </option>
                          {TaAssignePeople.map((user) => (
                            <option key={user.id} value={user.name}>
                              {user.name}
                            </option>
                          ))}
                        </select>
                      </div>
                      <span className="modal-label">Last Update</span>
                      <div>
                        <select
                          id="selector"
                          value={opcionSelecionada}
                          onChange={handleCambio}
                          className="selector-taassigne"
                        >
                          <option value="" disabled>
                            -- Select an option --
                          </option>
                          {lastUpdate.map((options, index) => (
                            <option key={index} value={options.option}>
                              {options.option}
                            </option>
                          ))}
                        </select>
                      </div>
                    </>
                  )}
                </div>
              </div>
            </div>
          </div>
          <div className="footer-coments">
            <div className="title-coments">
              <img className="img-coment" src={coment} alt=""></img>
              <span className="modal-value">Comments</span>
            </div>
            <div className="caja-comentarios">
              {/* Esta sección del textarea y el botón se quedará fija */}
              <textarea
                className="textarea-comment"
                value={commentText}
                onChange={(e) => setCommentext(e.target.value)}
                placeholder="Add a comment..."
              ></textarea>
              <div className="add-coment">
                <button className="add-comment-btn" onClick={handleAddComment}>
                  Add comment
                </button>
              </div>

              {/* Esta es la lista que se desplazará verticalmente */}
              <div className="comments-list-scroll">
                {Array.isArray(comments) &&
                  comments.map((comment) => (
                    <div key={comment.id} className="comment-item">
                      <span
                        className="comment-initials"
                        style={{ backgroundColor: comment.userColor }}
                      >
                        {comment.userInitials}
                      </span>
                      <div className="comentario-nombre">
                        <span>{comment.userName}</span>
                        <span className="comment-text">{comment.text}</span>
                      </div>
                      <div className="new-date">
                        <span>{formatDateTime(comment.timestamp)}</span>
                      </div>
                    </div>
                  ))}
              </div>
            </div>
          </div>
        </div>
      </div>
    </>
  );
}

export default Modal_MKT_RECR;
