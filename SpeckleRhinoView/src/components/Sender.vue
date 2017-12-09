<template>
  <v-card class='receiver-content blue-grey darken-4'>
    <!-- <v-divider></v-divider> -->
    <v-card-title primary-title class='pb-0 pt-3'>
      <div>
        <span class='headline'>
          <v-icon class='grey--text text--lighten-3'>cloud_upload</v-icon> {{ client.stream.name }}
        </span>
        <div class='caption'> <span class='grey--text'><code class='grey darken-2 white--text'>{{ client.stream.streamId }}</code></span> Last updated:  <timeago :auto-update='10' :since='client.lastUpdate'></timeago></div>
      </div>
    </v-card-title>

    <v-progress-linear height='3' :indeterminate='true' xxxv-if='client.isLoading'></v-progress-linear>
    <v-alert color='info' v-model='client.expired' >
      <v-layout align-center>
        <v-flex>There are updates available.</v-flex>
        <v-flex><v-btn dark small fab @click.native='refreshStream'><v-icon>refresh</v-icon></v-btn></v-flex>
      </v-layout>
    </v-alert>
    <v-card-actions>
      <v-btn icon @click.native='toggleLayers' fab small>
        <v-icon>{{ showLayers ? 'keyboard_arrow_up' : 'layers' }}</v-icon>
      </v-btn>
    </v-card-actions>
  </v-card>
</template>

<script>

  export default {
    name: 'Sender',
    props: {
      client: Object
    },
    components: {},
    computed: {},
    data() {
      return {
      }
    },
    methods: {
    },
    mounted() {
    }
  }
</script>

<style lang='scss'>
    
</style>