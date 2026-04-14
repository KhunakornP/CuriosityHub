<script setup lang="ts">
import { useRoute, useRouter } from 'vue-router'
import { ref, onMounted, onUnmounted, computed } from 'vue'
import { fetchVideoDetails, fetchVideoStreamBlobUrl } from '../apis/VideoService'
import { fetchComments, fetchReplies, postComment, type Comment, type CreateCommentRequest } from '../apis/CommentService'
import CommentPost from '../components/CommentPost.vue'

const route = useRoute()
const router = useRouter()

const videoId = route.params.id as string

const isFetchingVideo = ref(true)
const videoData = ref<any>(null)
const videoUrl = ref<string | null>(null)
const videoError = ref<string | null>(null)

const showFullDescription = ref(false)

const comments = ref<(Comment & { replies?: Comment[], showReplies?: boolean, isLoadingReplies?: boolean })[]>([])
const isPostingComment = ref(false)
const commentError = ref<string | null>(null)

const commentCharLimit = 1000

// For cleanup of Object URL to prevent memory leaks
onUnmounted(() => {
  if (videoUrl.value) {
    URL.revokeObjectURL(videoUrl.value)
  }
})

onMounted(async () => {
  isFetchingVideo.value = true
  try {
    const [details, streamUrl, loadedComments] = await Promise.all([
      fetchVideoDetails(videoId),
      fetchVideoStreamBlobUrl(videoId),
      fetchComments(videoId)
    ])
    
    if (details) videoData.value = details
    if (streamUrl) videoUrl.value = streamUrl
    if (loadedComments) comments.value = loadedComments.map(c => ({ ...c, showReplies: false }))
    
  } catch (err) {
    videoError.value = 'Failed to load video details'
    console.error(err)
  } finally {
    isFetchingVideo.value = false
  }
})

const navigateToHome = () => {
  router.push('/')
}

const toggleDescription = () => {
  showFullDescription.value = !showFullDescription.value
}

const toggleReplies = async (comment: any) => {
  if (comment.showReplies) {
    comment.showReplies = false
    return
  }

  if (!comment.replies) {
    comment.isLoadingReplies = true
    try {
      const fetchedReplies = await fetchReplies(comment.id)
      comment.replies = fetchedReplies
    } catch (err) {
      console.error('Failed to load replies:', err)
      comment.replies = [] // Graceful degradation
    } finally {
      comment.isLoadingReplies = false
    }
  }
  comment.showReplies = true
}

const submitComment = async (text: string) => {
  if (!text) return

  isPostingComment.value = true
  commentError.value = null

  try {
    const req: CreateCommentRequest = {
      user: 'AnonymousUser', // Replace with real user if authentication exists
      comment: text,
      parent_Comment: null,
      video_Id: videoId
    }

    const postedMsg = await postComment(req)
    if (postedMsg) {
      comments.value.unshift({ ...postedMsg, showReplies: false })
    } else {
      commentError.value = "Failed to post comment. Please try again."
    }
  } catch (err) {
    commentError.value = "A network error occurred while posting your comment."
    console.error('Failed to post comment:', err)
  } finally {
    isPostingComment.value = false
  }
}

// Reply functionalties
const activeReplyCommentId = ref<string | null>(null)
const isPostingReply = ref(false)
const replyError = ref<string | null>(null)

const openReplyInput = (commentId: string) => {
  activeReplyCommentId.value = activeReplyCommentId.value === commentId ? null : commentId
  replyError.value = null
}

const submitReply = async (parentComment: any, text: string) => {
  if (!text) return

  isPostingReply.value = true
  replyError.value = null

  try {
    const req: CreateCommentRequest = {
      user: 'AnonymousUser',
      comment: text,
      parent_Comment: parentComment.id,
      video_Id: videoId
    }

    const postedMsg = await postComment(req)
    if (postedMsg) {
      // If replies aren't loaded yet, just mark as having replies and optionally load them
      if (!parentComment.replies) {
        parentComment.replies = []
      }
      parentComment.replies.push(postedMsg)
      parentComment.nested = true
      parentComment.showReplies = true
      activeReplyCommentId.value = null
    } else {
      replyError.value = "Failed to post reply. Please try again."
    }
  } catch (err) {
    replyError.value = "A network error occurred while posting your reply."
    console.error('Failed to post reply:', err)
  } finally {
    isPostingReply.value = false
  }
}

const formatDate = (dateString: string) => {
  const date = new Date(dateString)
  return isNaN(date.getTime()) ? 'Unknown Date' : date.toLocaleDateString()
}
</script>

<template>
  <div class="min-h-screen bg-black text-white p-6 pt-4">
    <header class="mb-4 flex justify-end items-center">
      <button class="px-4 py-2 text-sm bg-neutral-800 rounded-full hover:bg-neutral-700 transition" @click="navigateToHome">
        Back to Home
      </button>
    </header>

    <div class="flex flex-col lg:flex-row gap-6">
      <!-- Main Video Area -->
      <div class="lg:w-3/4 flex-grow">
        <!-- Error State For Video Fetch -->
        <div v-if="videoError" class="bg-red-900/20 border border-red-500/50 text-red-500 p-6 rounded-xl text-center mb-6">
          <svg class="w-12 h-12 mx-auto mb-3 text-red-400" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path>
          </svg>
          <h2 class="text-xl font-bold mb-2">Video Unavailable</h2>
          <p>{{ videoError }}</p>
          <button @click="navigateToHome" class="mt-4 px-6 py-2 bg-red-600 hover:bg-red-700 text-white rounded-full transition-colors">
            Return to Home
          </button>
        </div>

        <div v-else class="aspect-video bg-neutral-900 rounded-xl flex items-center justify-center border border-neutral-800 shadow-lg overflow-hidden">
          <div v-if="isFetchingVideo" class="animate-spin rounded-full h-8 w-8 border-t-2 border-b-2 border-blue-500"></div>
          <video v-else-if="videoUrl" :src="videoUrl" controls autoplay class="w-full h-full object-cover">
            Your browser does not support the video tag.
          </video>
          <p v-else class="text-neutral-500">Video not found or failed to load (ID: {{ videoId }})</p>
        </div>
        
        <div v-if="!videoError" class="mt-4">
          <h2 class="text-2xl font-semibold mb-2" v-if="!isFetchingVideo">
            {{ videoData?.metadata?.title || 'Video Title Placeholder' }}
          </h2>
          <div class="flex items-center justify-between mb-4 border-b border-neutral-800 pb-4">
            <div class="flex items-center gap-4 text-sm text-neutral-400">
              <span class="text-white hover:text-blue-400 transition-colors cursor-pointer">
                {{ videoData?.metadata?.channelName || 'Unknown Channel' }}
              </span>
              <span>{{ videoData?.views?.count || '0' }} views</span>
              <span>{{ videoData?.metadata?.publishedAt || 'Unknown Date' }}</span>
            </div>
            <!-- Removed Likes and Shares -->
          </div>
          <div class="bg-neutral-900 p-4 rounded-xl text-sm leading-relaxed text-neutral-300">
            <div :class="{'line-clamp-2': !showFullDescription}">
              {{ videoData?.metadata?.description || 'No description available.' }}
            </div>
            <button 
              v-if="videoData?.metadata?.description && videoData?.metadata?.description.length > 100" 
              class="text-neutral-400 hover:text-white mt-1 text-xs font-semibold"
              @click="toggleDescription"
            >
              {{ showFullDescription ? 'Show Less' : 'Show More' }}
            </button>
          </div>
          
          <!-- Comment Section -->
          <div class="mt-8 border-t border-neutral-800 pt-6">
            <h3 class="text-xl font-semibold mb-4">{{ comments.length }} Comments</h3>
            
            <div class="mb-8">
              <CommentPost 
                :isPosting="isPostingComment"
                :error="commentError"
                @submit="submitComment"
              />
            </div>

            <!-- Dynamic Comments -->
            <div class="space-y-6">
              <div v-for="comment in comments" :key="comment.id" class="flex flex-col gap-2">
                <div class="flex gap-4">
                  <div class="w-10 h-10 rounded-full bg-neutral-800 flex-shrink-0"></div>
                  <div>
                    <div class="flex items-center gap-2 mb-1">
                      <span class="text-sm font-semibold text-white">@{{ comment.userId }}</span>
                      <span class="text-xs text-neutral-500">{{ formatDate(comment.createdAt) }}</span>
                    </div>
                    <p class="text-sm text-neutral-300 break-words whitespace-pre-wrap">
                      {{ comment.text }}
                    </p>
                    <div class="flex items-center gap-4 mt-2 text-neutral-400">
                      <!-- Removed Like -->
                      <button class="hover:text-blue-400" @click="openReplyInput(comment.id)"><span class="text-xs font-medium">Reply</span></button>
                    </div>
                    
                    <!-- Reply Input -->
                    <div v-if="activeReplyCommentId === comment.id" class="mt-4 ml-8">
                      <CommentPost 
                        :isPosting="isPostingReply"
                        :error="replyError"
                        placeholder="Add a reply..."
                        buttonText="Reply"
                        @submit="(text) => submitReply(comment, text)"
                        @cancel="activeReplyCommentId = null"
                      />
                    </div>

                    <!-- View Replies Button block conditioned on nested -->
                    <div v-if="comment.nested" class="mt-2 text-blue-400 hover:text-blue-300 text-sm font-medium cursor-pointer" @click="toggleReplies(comment)">
                      <div v-if="comment.isLoadingReplies" class="flex gap-2 items-center text-neutral-400">
                        <div class="w-3 h-3 border-2 border-current border-t-transparent rounded-full animate-spin"></div>
                        <span>Loading...</span>
                      </div>
                      <span v-else>
                        {{ comment.showReplies ? 'Hide replies' : 'View replies' }}
                      </span>
                    </div>

                    <!-- Replies list -->
                    <div v-if="comment.showReplies && comment.replies && comment.replies.length > 0" class="mt-4 space-y-4 pl-4 border-l-2 border-neutral-800">
                      <div v-for="reply in comment.replies" :key="reply.id" class="flex gap-3">
                        <div class="w-8 h-8 rounded-full bg-neutral-800 flex-shrink-0"></div>
                        <div>
                          <div class="flex items-center gap-2 mb-1">
                            <span class="text-sm font-semibold text-white">@{{ reply.userId }}</span>
                            <span class="text-xs text-neutral-500">{{ formatDate(reply.createdAt) }}</span>
                          </div>
                          <p class="text-xs text-neutral-300 break-words whitespace-pre-wrap">
                            {{ reply.text }}
                          </p>
                        </div>
                      </div>
                    </div>
                    <div v-else-if="comment.showReplies && comment.replies && comment.replies.length === 0" class="mt-2 pl-4 text-xs text-neutral-500">
                      No replies yet.
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
      
      <!-- Side Recommendations Panel -->
      <div class="lg:w-1/4 flex-shrink-0 flex flex-col gap-4">
        <h3 class="font-semibold text-lg border-b border-neutral-800 pb-2">Up Next</h3>
        
        <div v-for="i in 5" :key="i" class="flex gap-2 group cursor-pointer">
          <div class="w-40 aspect-video bg-neutral-800 rounded-lg flex-shrink-0 relative overflow-hidden group-hover:scale-105 transition-transform duration-200">
             <!-- Thumbnail Placeholder -->
          </div>
          <div class="flex flex-col py-1">
            <h4 class="text-sm font-semibold line-clamp-2 leading-tight group-hover:text-blue-400">
              Recommended Video Title {{ i }}
            </h4>
            <p class="text-xs text-neutral-400 mt-1">Channel {{ i }}</p>
            <p class="text-xs text-neutral-500">500K views</p>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
