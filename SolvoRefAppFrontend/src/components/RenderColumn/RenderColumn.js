import { Droppable } from "@hello-pangea/dnd";
const RenderColumn = ({ columnId, title, children, quantity = 0 }) => {
  return (
    <Droppable droppableId={columnId}>
      {(provided, snapshot) => (
        <div
          className={`cards-8 column card ${
            snapshot.isDraggingOver ? "dragging-over" : ""
          }`}
          {...provided.droppableProps}
          ref={provided.innerRef}
        >
          <div className="column-title">
            <h2>{title}</h2>
            <span>{quantity}</span>
          </div>

          <hr className="hr" />
          {children}
          {provided.placeholder}
        </div>
      )}
    </Droppable>
  );
};

export default RenderColumn;
