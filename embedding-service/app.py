from collections.abc import AsyncIterator
from contextlib import asynccontextmanager
from typing import Annotated, cast

from fastapi import Body, FastAPI, Request
from sentence_transformers import SentenceTransformer

MODEL_NAME = "sentence-transformers/all-MiniLM-L6-v2"


@asynccontextmanager
async def lifespan(app: FastAPI) -> AsyncIterator[None]:
    app.state.model = SentenceTransformer(MODEL_NAME)
    yield
    del app.state.model


app = FastAPI(
    lifespan=lifespan,
    docs_url=None,
    redoc_url=None,
    openapi_url=None,
)


@app.post("/embed")
def embed(
    texts: Annotated[list[str], Body()],
    request: Request,
) -> list[list[float]]:
    model = cast(SentenceTransformer, request.app.state.model)
    return model.encode(texts, convert_to_numpy=True).tolist()
