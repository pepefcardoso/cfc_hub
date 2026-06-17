import React from "react";
import { Button } from "@/components/ui/button";
import { Loader2 } from "lucide-react";

interface CursorPaginationProps {
  hasMore: boolean;
  nextCursor?: string | null;
  onNext: (cursor: string) => void;
  isLoading?: boolean;
}

export function CursorPagination({
  hasMore,
  nextCursor,
  onNext,
  isLoading,
}: CursorPaginationProps) {
  if (!hasMore) {
    return null;
  }

  return (
    <div className="flex justify-center mt-6">
      <Button
        variant="outline"
        onClick={() => nextCursor && onNext(nextCursor)}
        disabled={isLoading || !nextCursor}
      >
        {isLoading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
        Carregar mais
      </Button>
    </div>
  );
}
