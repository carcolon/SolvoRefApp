import React, {
  useState,
  useMemo,
  useRef,
  useEffect,
  useCallback,
} from "react";
import { Droppable, Draggable } from "@hello-pangea/dnd";
import "./CardsRecr.css";
import ModalDetalleCardsMKT_RECR from "../ModalCopmponent/ModalDetalleCardsMKT_RECR";
import { Tooltip } from "react-tooltip";
import { formatTimeElapsed } from "../CostantsComponent/timeElapsed";
import { getInitials } from "../CostantsComponent/getInitials";
const COLORS_ARRAY = [
  "#98b46d",
  "#00b4b4",
  "#ff7f50",
  "#F4D03F",
  "#A3D8A3",
  "#F9E79F",
  "#A9CCE3",
  "#D7BDE2",
];

const getUserColor = (name, users) => {
  const index = users.findIndex((user) => user.name === name);
  if (index === -1) {
    return "#808080";
  }
  return COLORS_ARRAY[index % COLORS_ARRAY.length];
};

const RecrCard = ({
  user,
  provided,
  recruiterUsers,
  handleUserUpdate,
  onCardClick,
}) => {
  const [isSelectorOpen, setIsSelectorOpen] = useState(false);
  const [recruiterFilter, setRecruiterFilter] = useState("");
  const selectorRef = useRef(null);

  const assignedRecruiterUser = useMemo(() => {
    if (user.assignedRecruiterId) {
      return recruiterUsers.find((rec) => rec.id === user.assignedRecruiterId);
    }
    return null;
  }, [user.assignedRecruiterId, recruiterUsers]);

  const initials = assignedRecruiterUser
    ? getInitials(assignedRecruiterUser.name)
    : getInitials(user.name) || "US";
  const color = assignedRecruiterUser
    ? getUserColor(assignedRecruiterUser.name, recruiterUsers)
    : getUserColor(user.name, recruiterUsers);

  const showTimeBadge =
    user.status === "In Progress" || user.status === "First Contact";
  const handleRecruiterInitialsClick = useCallback((e) => {
    e.stopPropagation();
    setIsSelectorOpen((prev) => !prev);
    setRecruiterFilter("");
  }, []);

  const handleUserSelect = useCallback(
    (e, selectedUser) => {
      if (e) {
        e.stopPropagation();
      }
      handleUserUpdate(user.id, selectedUser);
      setIsSelectorOpen(false);
      setRecruiterFilter("");
    },
    [handleUserUpdate, user.id]
  );

  useEffect(() => {
    const handleClickOutside = (event) => {
      if (selectorRef.current && !selectorRef.current.contains(event.target)) {
        setIsSelectorOpen(false);
        setRecruiterFilter("");
      }
    };
    document.addEventListener("mousedown", handleClickOutside);
    return () => {
      document.removeEventListener("mousedown", handleClickOutside);
    };
  }, []);
  const filteredRecruiterUsers = useMemo(() => {
    if (!recruiterFilter) {
      return recruiterUsers;
    }
    const lowercasedFilter = recruiterFilter.toLowerCase();
    return recruiterUsers.filter((u) =>
      u.name.toLowerCase().includes(lowercasedFilter)
    );
  }, [recruiterUsers, recruiterFilter]);
  return (
    <div
      className={`movable-item ${user.status
        .toLowerCase()
        .replace(/\s/g, "-")}-card`}
      ref={provided.innerRef}
      {...provided.draggableProps}
      {...provided.dragHandleProps}
      onClick={() => onCardClick(user)}
    >
      <div className="custom-card-content">
        <div className="card-heade">
          <h3 className="card-name">{user.name}</h3>
          <span className="span-enmail">{user.email}</span>
          <br />
          <span>{user.phoneNumber}</span>
        </div>

        {user.referredBy && (
          <div className="card-referred-by">
            <span className="label">Referred by:</span>
            <span className="value-refereby"> {user.referredBy}</span>
            {user.status === "In Progress" && (
              <labe>
                Recoverable
                <input type="checkbox" onClick={(e) => e.stopPropagation()} />
              </labe>
            )}
          </div>
        )}

        <div className="card-foote">
          <p className="card-dat">{user.date}</p>
          {showTimeBadge && (
            <div className="in-progress-badge">
              <span role="img" aria-label="clock"></span>
              <span className="card-met">
                {formatTimeElapsed(user.creationDate)}
              </span>
            </div>
          )}
        </div>

        {/* Selector de Reclutadores */}
        <div className="card-initials-selector-container">
          <div
            className="card-initials-display"
            style={{ backgroundColor: color }}
            onClick={handleRecruiterInitialsClick}
            data-tooltip-id={`recruiter-tooltip-${user.id}`}
            data-tooltip-content={
              assignedRecruiterUser
                ? assignedRecruiterUser.name
                : "Select Recruiter"
            }
            data-tooltip-place="bottom"
          >
            <h6>{initials}</h6>
          </div>
          <Tooltip id={`recruiter-tooltip-${user.id}`} />

          {isSelectorOpen && (
            <div
              style={{ width: "30px", right: "-20px" }}
              className="mkt-users-dropdown"
              ref={selectorRef}
            >
              <input
                style={{ width: "150px", position: "relative", right: "-10px" }}
                type="text"
                placeholder="Filter recruiters..."
                value={recruiterFilter}
                onChange={(e) => setRecruiterFilter(e.target.value)}
                onClick={(e) => e.stopPropagation()}
              />
              <ul>
                {filteredRecruiterUsers.length > 0 ? (
                  filteredRecruiterUsers.map((recruiterUser) => (
                    <li
                      key={recruiterUser.id}
                      onClick={(e) => handleUserSelect(e, recruiterUser)}
                      className={
                        assignedRecruiterUser &&
                        assignedRecruiterUser.id === recruiterUser.id
                          ? "selected"
                          : ""
                      }
                    >
                      <div
                        className="mkt-user-option-initials"
                        style={{
                          backgroundColor: getUserColor(
                            recruiterUser.name,
                            recruiterUsers
                          ),
                        }}
                      >
                        {getInitials(recruiterUser.name)}
                      </div>
                      <span className="mkt-user-option-name">
                        {recruiterUser.name}
                      </span>
                    </li>
                  ))
                ) : (
                  <div className="no-results-message">No recruiters found</div>
                )}
              </ul>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};
function CardRecr({
  inProgressUsers,
  firstContactUsers,
  taInterviewUsers,
  noResponseUsers,
  rejectedUsers,
  hiredUsers,
  handleUserUpdate,
  recruiterUsers,
  columns,
}) {
  // Estado para la ventana modal
  const [selectedCard, setSelectedCard] = useState(null);

  // Funciones para manejar la modal
  const handleCardClick = useCallback((card) => {
    setSelectedCard(card);
  }, []);

  const handleCloseModal = useCallback(() => {
    setSelectedCard(null);
  }, []);

  const renderCards = useCallback(
    (users) => {
      if (!users || users.length === 0) {
        return <p className="no-cards-message"></p>;
      }

      return users.map((user, index) => (
        <Draggable key={user.id} draggableId={user.id} index={index}>
          {(provided) => (
            <RecrCard
              user={user}
              provided={provided}
              recruiterUsers={recruiterUsers}
              handleUserUpdate={handleUserUpdate}
              onCardClick={handleCardClick}
            />
          )}
        </Draggable>
      ));
    },
    [recruiterUsers, handleUserUpdate, handleCardClick]
  );

  const renderColumn = (columnId, title, users) => (
    <Droppable droppableId={columnId}>
      {(provided, snapshot) => (
        <div
          className={`cards-8 column card ${
            snapshot.isDraggingOver ? "dragging-over" : ""
          }`}
          {...provided.droppableProps}
          ref={provided.innerRef}
        >
          <div className="title-count">
            <h2 className="column-title">{title}</h2>
            <span>{users.length}</span>
          </div>
          <hr className="hr" />
          {renderCards(users)}
          {provided.placeholder}
        </div>
      )}
    </Droppable>
  );

  return (
    <div className="contenedor">
      {renderColumn("in-progress", "In Progress", inProgressUsers)}
      {renderColumn("first-contact", "First Contact", firstContactUsers)}
      {renderColumn("ta-interview", "TA Interview", taInterviewUsers)}
      {renderColumn("no-response", "No Response", noResponseUsers)}
      {renderColumn("rejected", "Rejected", rejectedUsers)}
      {renderColumn("hired", "Hired", hiredUsers)}

      {selectedCard && (
        // eslint-disable-next-line react/jsx-pascal-case
        <ModalDetalleCardsMKT_RECR
          card={selectedCard}
          onClose={handleCloseModal}
          recruiterUsers={recruiterUsers}
          view="recr"
        />
      )}
    </div>
  );
}

export default CardRecr;
